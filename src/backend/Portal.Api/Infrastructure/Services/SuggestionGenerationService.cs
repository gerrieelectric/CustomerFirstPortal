using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Application.Infrastructure.Persistence;
using Portal.Api.DTOs.Submissions;
using Portal.Domain.Entities;

namespace Portal.Application.Infrastructure.Services;

public class SuggestionGenerationOptions
{
    public bool Enabled { get; set; } = true;
    public string ModelName { get; set; } = "gpt-4o-mini";
    public int PromptVersion { get; set; } = 1;
    public int MaxTokens { get; set; } = 500;
    public float Temperature { get; set; } = 0.3f;
}

public class SuggestionGenerationService : ISuggestionGenerationService
{
    private readonly PortalDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SuggestionGenerationOptions _options;
    private readonly ILogger<SuggestionGenerationService> _logger;
    private readonly string _azureOpenAiEndpoint;
    private readonly string _azureOpenAiApiKey;

    public SuggestionGenerationService(
        PortalDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IOptions<SuggestionGenerationOptions> options,
        ILogger<SuggestionGenerationService> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;

        // Get Azure OpenAI configuration
        var azureOpenAiConfig = configuration.GetSection("AzureOpenAI");
        _azureOpenAiEndpoint = azureOpenAiConfig["Endpoint"] ?? "";
        _azureOpenAiApiKey = azureOpenAiConfig["ApiKey"] ?? "";
    }

    public async Task<SubmissionSuggestion?> GenerateSuggestionsAsync(
        Submission submission,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Suggestion generation disabled in configuration.");
            return null;
        }

        try
        {
            _logger?.LogInformation("Beginning suggestion generation for submission {SubmissionId}", submission.SubmissionId);

            var reasonHash = ComputeHash(submission.RecognitionReason);
            _logger?.LogDebug("Computed hash for submission {SubmissionId}", submission.SubmissionId);

            // Check if suggestions already exist for this reason
            var existing = await _dbContext.SubmissionSuggestions
                .FirstOrDefaultAsync(
                    s => s.SubmissionId == submission.SubmissionId
                        && s.ReasonHash == reasonHash
                        && s.Status == "Success",
                    cancellationToken);

            if (existing != null)
            {
                _logger?.LogInformation(
                    "Suggestion snapshot already exists for submission {SubmissionId}.",
                    submission.SubmissionId);
                return existing;
            }

            // Generate suggestions from Azure OpenAI
            var (editedReason, suggestions) = await CallAzureOpenAiAsync(
                submission.RecognitionReason,
                submission.NomineeName,
                cancellationToken);

            // Serialize suggestions to JSON
            var suggestionsJson = JsonSerializer.Serialize(suggestions);

            // Create and persist the snapshot
            var suggestion = new SubmissionSuggestion
            {
                SubmissionSuggestionId = Guid.NewGuid(),
                SubmissionId = submission.SubmissionId,
                ReasonHash = reasonHash,
                OriginalReason = submission.RecognitionReason,
                FullEditedReason = editedReason,
                SuggestionsJson = suggestionsJson,
                ModelName = _options.ModelName,
                PromptVersion = _options.PromptVersion,
                GeneratedUtc = DateTime.UtcNow,
                Status = suggestions.Count > 0 ? "Success" : "Success", // Even if no changes, still success
                ErrorMessage = null
            };

            _dbContext.SubmissionSuggestions.Add(suggestion);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger?.LogInformation(
                "Generated suggestions for submission {SubmissionId}: {SuggestionCount} changes.",
                submission.SubmissionId,
                suggestions.Count);

            return suggestion;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to generate suggestions for submission {SubmissionId}.",
                submission.SubmissionId);

            // Create a failed suggestion snapshot so we don't retry indefinitely
            var failedSuggestion = new SubmissionSuggestion
            {
                SubmissionSuggestionId = Guid.NewGuid(),
                SubmissionId = submission.SubmissionId,
                ReasonHash = ComputeHash(submission.RecognitionReason),
                OriginalReason = submission.RecognitionReason,
                FullEditedReason = submission.RecognitionReason, // Fallback to original
                SuggestionsJson = "[]",
                ModelName = _options.ModelName,
                PromptVersion = _options.PromptVersion,
                GeneratedUtc = DateTime.UtcNow,
                Status = "Failed",
                ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 500)]
            };

            try
            {
                _dbContext.SubmissionSuggestions.Add(failedSuggestion);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception persistEx)
            {
                _logger.LogError(persistEx, "Failed to persist failure snapshot for submission {SubmissionId}.", submission.SubmissionId);
            }

            return null;
        }
    }

    public async Task<SubmissionSuggestion?> GetSuggestionsAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.SubmissionSuggestions
            .FirstOrDefaultAsync(s => s.SubmissionId == submissionId, cancellationToken);
    }

    public async Task<SubmissionSuggestion?> RegenerateSuggestionsAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        var submission = await _dbContext.Submissions
            .FirstOrDefaultAsync(s => s.SubmissionId == submissionId, cancellationToken);

        if (submission == null)
        {
            _logger.LogWarning("Submission {SubmissionId} not found for regeneration.", submissionId);
            return null;
        }

        // Remove old suggestions for this submission
        var oldSuggestions = await _dbContext.SubmissionSuggestions
            .Where(s => s.SubmissionId == submissionId)
            .ToListAsync(cancellationToken);

        if (oldSuggestions.Count > 0)
        {
            _dbContext.SubmissionSuggestions.RemoveRange(oldSuggestions);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Generate fresh suggestions
        return await GenerateSuggestionsAsync(submission, cancellationToken);
    }

    private async Task<(string editedReason, List<EditSuggestion> suggestions)> CallAzureOpenAiAsync(
        string recognitionReason,
        string nomineeName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_azureOpenAiEndpoint) || string.IsNullOrEmpty(_azureOpenAiApiKey))
        {
            _logger.LogError("Azure OpenAI configuration is missing (endpoint or API key).");
            throw new InvalidOperationException("Azure OpenAI is not properly configured.");
        }

        var prompt = BuildPrompt(recognitionReason, nomineeName);
        var requestBody = new
        {
            messages = new[]
            {
                new { role = "system", content = SystemPrompt() },
                new { role = "user", content = prompt }
            },
            max_tokens = _options.MaxTokens,
            temperature = _options.Temperature,
            top_p = 0.95,
            frequency_penalty = 0,
            presence_penalty = 0,
        };

        var client = _httpClientFactory.CreateClient();
        var requestUri = $"{_azureOpenAiEndpoint.TrimEnd('/')}/openai/deployments/{_options.ModelName}/chat/completions?api-version=2024-02-15-preview";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Add("api-key", _azureOpenAiApiKey);

        using var response = await client.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Azure OpenAI API returned {StatusCode}: {Response}",
                response.StatusCode,
                responseContent[..Math.Min(responseContent.Length, 500)]);
            throw new HttpRequestException($"Azure OpenAI API failed: {response.StatusCode}");
        }

        var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;

        // Extract response from Azure OpenAI format
        string messageContent;
        if (root.TryGetProperty("choices", out var choicesElement) && choicesElement.ValueKind == JsonValueKind.Array)
        {
            var choice = choicesElement.EnumerateArray().FirstOrDefault();
            if (choice.TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("content", out var contentElement))
            {
                messageContent = contentElement.GetString() ?? "";
            }
            else
            {
                throw new JsonException("Unexpected Azure OpenAI response format");
            }
        }
        else
        {
            throw new JsonException("Unexpected Azure OpenAI response format");
        }

        var (editedReason, suggestions) = ParseResponse(messageContent, recognitionReason);
        return (editedReason, suggestions);
    }

    private string SystemPrompt()
    {
        return """
You are an expert editor specializing in minimal, precise suggestions for employee recognition reasons.
Your task is to identify ONLY minimal edits needed for:
1. Spelling errors
2. Grammar and punctuation
3. Proper name corrections (especially employee names) - IMPORTANT: Only correct actual misspellings of the nominee's name, NOT intentional shortforms or nicknames
4. Formatting and capitalization
5. Professionalism improvements: replace slang, acronyms, and shortforms with professional equivalents
6. Sentence structure: fix run-on sentences, add missing punctuation, remove excessive punctuation

CRITICAL NAME HANDLING RULES:
- The nominee's full name is provided in the context below
- If the recognition reason uses an intentional shortform or nickname (like "Zack" for "Zachary"), KEEP IT - do not suggest changing it
- ONLY correct actual misspellings of the full name (e.g., "Zachery" → "Zachary")
- Common nicknames: Rob/Robert, Tom/Thomas, Mike/Michael, Rick/Richard, etc. - KEEP these if used
- If uncertain whether something is intentional, DO NOT suggest a correction

Examples of professionalism edits (maintain meaning and sentiment):
- "tho" → "though"
- "ASAP" → "as soon as possible"
- "U did great" → "You did great"
- "gonna help" → "will help"
- "no prob" → "no problem"
- "BTW" → "By the way"

Examples of sentence structure edits (maintain meaning and sentiment):
- "he did great work he finished early" → "He did great work and finished early." (fix run-on)
- "The employee went extra" → "The employee went the extra mile." (add context)
- "Amazing!!!!!" → "Amazing!" (remove excessive punctuation)
- "great work" → "Great work." (add period)
- "helped me out; real quick" → "helped me out quickly." (fix punctuation and clarity)

Examples of NAME corrections (actual misspellings only):
- Nominee is "Zachary VanderBurgt": "Zachery" → "Zachary" (correct misspelling, KEEP "Zack" if used)
- Nominee is "Jennifer Martinez": "Jenniffer" → "Jennifer" (correct misspelling, KEEP "Jen" if used)

You MUST NOT:
- Rewrite sentences or change meaning
- Suggest style improvements or rewording beyond professionalism and structure
- Change the tone or sentiment of the message
- Suggest removing or adding content
- Suggest changing intentional nicknames or shortforms

Return your response in structured JSON format with exactly this structure:
{
  "edits": [
    { "original": "exact phrase", "suggested": "corrected phrase", "category": "spelling|grammar|name|formatting|professionalism|sentence_structure", "position": 0 }
  ],
  "editedFull": "The corrected recognition reason text only, no headers or labels"
}

If no edits are needed, return:
{
  "edits": [],
  "editedFull": "[ORIGINAL_TEXT_HERE]"
}
""";
    }

    private string BuildPrompt(string recognitionReason, string nomineeName)
    {
        return $"""
Please review this employee recognition reason for minimal edits only.

IMPORTANT CONTEXT - The nominee's full name is: {nomineeName}
- Keep any intentional nicknames or shortforms of this name (like "Zack" for "Zachary")
- Only correct actual misspellings of the full name
- Use this context to distinguish between intentional shortforms and mistakes

Look for:
- Spelling errors and typos (but not intentional nicknames)
- Grammar issues
- Informal language, slang, acronyms, and shortforms
- Sentence structure problems, run-on sentences, and missing/excessive punctuation
- Capitalization and formatting issues

Focus on adding or removing punctuation as needed to improve clarity and professionalism while keeping the original meaning:

{recognitionReason}

Return your response in JSON format as specified.
""";
    }

    private (string editedReason, List<EditSuggestion> suggestions) ParseResponse(
        string responseContent,
        string originalReason)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            var editedFull = root.GetProperty("editedFull").GetString() ?? originalReason;

            var editsList = new List<EditSuggestion>();
            if (root.TryGetProperty("edits", out var editsElement) && editsElement.ValueKind == JsonValueKind.Array)
            {
                var position = 0;
                foreach (var edit in editsElement.EnumerateArray())
                {
                    var suggestion = new EditSuggestion
                    {
                        Original = edit.GetProperty("original").GetString() ?? "",
                        Suggested = edit.GetProperty("suggested").GetString() ?? "",
                        Category = edit.GetProperty("category").GetString() ?? "other",
                        Position = edit.TryGetProperty("position", out var posElement)
                            ? posElement.GetInt32()
                            : position
                    };
                    editsList.Add(suggestion);
                    position++;
                }
            }

            return (editedFull, editsList);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to parse Azure OpenAI response as JSON. Response: {Response}",
                responseContent[..Math.Min(responseContent.Length, 500)]);

            // Fallback: return original reason with no suggestions
            return (originalReason, []);
        }
    }

    private static string ComputeHash(string text)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hashedBytes).ToLowerInvariant();
    }
}
