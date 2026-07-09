using Portal.Domain.Entities;

namespace Portal.Application.Infrastructure.Services;

public interface ISuggestionGenerationService
{
    /// <summary>
    /// Generates AI suggestions for a submission's recognition reason and persists them to the database.
    /// Idempotent based on submission ID and reason hash - won't regenerate for unchanged reasons.
    /// </summary>
    /// <param name="submission">The submission to generate suggestions for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or retrieved SubmissionSuggestion entity.</returns>
    Task<SubmissionSuggestion?> GenerateSuggestionsAsync(Submission submission, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves persisted suggestions for a submission, or null if not yet generated.
    /// </summary>
    Task<SubmissionSuggestion?> GetSuggestionsAsync(Guid submissionId, CancellationToken cancellationToken);

    /// <summary>
    /// Manually regenerates suggestions for a submission, even if one already exists.
    /// Used for admin override if suggestions are stale or incorrect.
    /// </summary>
    Task<SubmissionSuggestion?> RegenerateSuggestionsAsync(Guid submissionId, CancellationToken cancellationToken);
}
