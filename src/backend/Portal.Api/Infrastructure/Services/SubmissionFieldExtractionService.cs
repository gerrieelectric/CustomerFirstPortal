using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Options;
using MimeKit;
using Portal.Api.DTOs.Submissions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Portal.Application.Infrastructure.Services;

public class FieldExtractionOptions
{
    public bool Enabled { get; set; } = true;
    public string ModelName { get; set; } = "gpt-4o-mini";
    public int MaxTokens { get; set; } = 800;
    public float Temperature { get; set; } = 0.1f;
    public int MaxCharsPerFile { get; set; } = 8000;
}

public class SubmissionFieldExtractionService : ISubmissionFieldExtractionService
{
    private static readonly string[] ImageExtensions = ["png", "jpg", "jpeg"];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FieldExtractionOptions _options;
    private readonly ILogger<SubmissionFieldExtractionService> _logger;
    private readonly string _azureOpenAiEndpoint;
    private readonly string _azureOpenAiApiKey;

    public SubmissionFieldExtractionService(
        IHttpClientFactory httpClientFactory,
        IOptions<FieldExtractionOptions> options,
        ILogger<SubmissionFieldExtractionService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;

        var azureOpenAiConfig = configuration.GetSection("AzureOpenAI");
        _azureOpenAiEndpoint = azureOpenAiConfig["Endpoint"] ?? "";
        _azureOpenAiApiKey = azureOpenAiConfig["ApiKey"] ?? "";
    }

    public async Task<ExtractedSubmissionFieldsResponse> ExtractFieldsAsync(
        IReadOnlyList<ExtractionAttachment> attachments,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return new ExtractedSubmissionFieldsResponse { Status = "Disabled" };
        }

        if (string.IsNullOrEmpty(_azureOpenAiEndpoint) || string.IsNullOrEmpty(_azureOpenAiApiKey))
        {
            _logger.LogWarning("Field extraction requested but Azure OpenAI is not configured.");
            return new ExtractedSubmissionFieldsResponse { Status = "Disabled" };
        }

        try
        {
            var userContentParts = BuildUserContentParts(attachments);

            if (userContentParts.Count == 0)
            {
                _logger.LogInformation("No extractable content found in {Count} attachment(s).", attachments.Count);
                return new ExtractedSubmissionFieldsResponse { Status = "Failed" };
            }

            return await CallAzureOpenAiAsync(userContentParts, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Field extraction failed for {Count} attachment(s).", attachments.Count);
            return new ExtractedSubmissionFieldsResponse { Status = "Failed" };
        }
    }

    private List<object> BuildUserContentParts(IReadOnlyList<ExtractionAttachment> attachments)
    {
        var parts = new List<object>();

        foreach (var attachment in attachments)
        {
            var extension = Path.GetExtension(attachment.FileName).TrimStart('.').ToLowerInvariant();

            try
            {
                if (ImageExtensions.Contains(extension))
                {
                    var mimeType = extension == "png" ? "image/png" : "image/jpeg";
                    parts.Add(new
                    {
                        type = "text",
                        text = $"--- Attachment (image): {attachment.FileName} ---"
                    });
                    parts.Add(new
                    {
                        type = "image_url",
                        image_url = new
                        {
                            url = $"data:{mimeType};base64,{Convert.ToBase64String(attachment.Content)}"
                        }
                    });
                    continue;
                }

                var text = extension switch
                {
                    "txt" => Encoding.UTF8.GetString(attachment.Content),
                    "eml" => ExtractEmlText(attachment.Content),
                    "msg" => ExtractMsgText(attachment.Content),
                    "docx" => ExtractDocxText(attachment.Content),
                    "pdf" => ExtractPdfText(attachment.Content),
                    _ => null
                };

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (text.Length > _options.MaxCharsPerFile)
                {
                    text = text[.._options.MaxCharsPerFile];
                }

                parts.Add(new
                {
                    type = "text",
                    text = $"--- Attachment: {attachment.FileName} ---\n{text}"
                });
            }
            catch (Exception ex)
            {
                // Skip unparseable files; extraction is best-effort.
                _logger.LogWarning(ex, "Could not extract content from attachment {FileName}.", attachment.FileName);
            }
        }

        return parts;
    }

    private static string ExtractEmlText(byte[] content)
    {
        using var stream = new MemoryStream(content);
        var message = MimeMessage.Load(stream);

        var sender = message.From.Mailboxes.FirstOrDefault();
        var body = message.TextBody ?? message.HtmlBody ?? "";

        var builder = new StringBuilder();
        builder.AppendLine($"From: {sender?.Name} <{sender?.Address}>");
        builder.AppendLine($"Subject: {message.Subject}");
        builder.AppendLine($"Date: {message.Date}");
        builder.AppendLine("Body:");
        builder.AppendLine(body);
        return builder.ToString();
    }

    private static string ExtractMsgText(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var message = new MsgReader.Outlook.Storage.Message(stream);

        var builder = new StringBuilder();
        builder.AppendLine($"From: {message.Sender?.DisplayName} <{message.Sender?.Email}>");
        builder.AppendLine($"Subject: {message.Subject}");
        builder.AppendLine("Body:");
        builder.AppendLine(message.BodyText ?? message.BodyHtml ?? "");
        return builder.ToString();
    }

    private static string ExtractDocxText(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var document = WordprocessingDocument.Open(stream, false);
        return document.MainDocumentPart?.Document?.Body?.InnerText ?? "";
    }

    private static string ExtractPdfText(byte[] content)
    {
        using var document = PdfDocument.Open(content);
        var builder = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            builder.AppendLine(ContentOrderTextExtractor.GetText(page));
        }
        return builder.ToString();
    }

    private async Task<ExtractedSubmissionFieldsResponse> CallAzureOpenAiAsync(
        List<object> userContentParts,
        CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt() },
                new { role = "user", content = userContentParts }
            },
            max_tokens = _options.MaxTokens,
            temperature = _options.Temperature,
            response_format = new { type = "json_object" }
        };

        var client = _httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildRequestUri())
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
                "Azure OpenAI API returned {StatusCode} during field extraction: {Response}",
                response.StatusCode,
                responseContent[..Math.Min(responseContent.Length, 500)]);
            return new ExtractedSubmissionFieldsResponse { Status = "Failed" };
        }

        using var jsonDoc = JsonDocument.Parse(responseContent);
        var messageContent = jsonDoc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        return ParseExtractionResponse(messageContent);
    }

    private string BuildRequestUri()
    {
        var endpoint = _azureOpenAiEndpoint.TrimEnd('/');

        // Config may hold either the resource endpoint or a full chat-completions URL.
        if (endpoint.Contains("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint;
        }

        return $"{endpoint}/openai/deployments/{_options.ModelName}/chat/completions?api-version=2024-02-15-preview";
    }

    private ExtractedSubmissionFieldsResponse ParseExtractionResponse(string messageContent)
    {
        var json = messageContent.Trim();

        // Strip markdown fences if the model added them despite json_object mode.
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = json.IndexOf('\n');
            var lastFence = json.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline >= 0 && lastFence > firstNewline)
            {
                json = json[(firstNewline + 1)..lastFence].Trim();
            }
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new ExtractedSubmissionFieldsResponse
        {
            CompanyName = GetTrimmedString(root, "companyName"),
            CustomerName = GetTrimmedString(root, "customerName"),
            CustomerEmail = GetTrimmedString(root, "customerEmail"),
            NomineeName = GetTrimmedString(root, "nomineeName"),
            RecognitionReason = GetTrimmedString(root, "recognitionReason"),
            Status = "Success"
        };
    }

    private static string? GetTrimmedString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = property.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string SystemPrompt()
    {
        return """
You extract structured data from documents attached to a customer-recognition nomination form.

Context: an employee at our company is nominating a colleague (the NOMINEE) who provided
outstanding service to an external CUSTOMER. The attachments are typically emails, letters,
or screenshots in which a customer praises the employee.

From the attachment content provided, extract:
- companyName: the external customer's company/organization name. May be inferred from an
  email signature, letterhead, or the domain of the customer's email address (e.g.
  "john.smith@examplecompany.com" suggests "Example Company"). Do NOT use our own company.
- customerName: the full name of the external customer contact who gave the praise
  (e.g. the email sender).
- customerEmail: that contact's email address.
- nomineeName: the full name of the employee being praised.
- recognitionReason: the text describing what the employee did, taken from the source with
  minimal cleanup. Maximum 2000 characters. Keep the original wording and sentiment.

Rules:
- Return null for any field you cannot determine with reasonable confidence.
- NEVER invent names, email addresses, or companies that do not appear in the content.
- If multiple attachments conflict, prefer the most complete/explicit source.

Respond with ONLY a JSON object in exactly this shape:
{
  "companyName": string or null,
  "customerName": string or null,
  "customerEmail": string or null,
  "nomineeName": string or null,
  "recognitionReason": string or null
}
""";
    }
}
