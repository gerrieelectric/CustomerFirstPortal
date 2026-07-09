namespace Portal.Domain.Entities;

/// <summary>
/// Persisted AI-generated suggestions for a submission's recognition reason.
/// Generated once and reused on all subsequent views to minimize API costs.
/// </summary>
public class SubmissionSuggestion
{
    public Guid SubmissionSuggestionId { get; set; }
    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;

    /// <summary>
    /// Hash of the RecognitionReason at time of generation (MD5 or SHA256).
    /// Used to detect if reason changed and regeneration is needed.
    /// </summary>
    public string ReasonHash { get; set; } = string.Empty;

    /// <summary>
    /// The original RecognitionReason text that was processed.
    /// Stored for audit and comparison.
    /// </summary>
    public string OriginalReason { get; set; } = string.Empty;

    /// <summary>
    /// The fully edited recognition reason with all suggestions applied.
    /// Ready to copy/use as-is if admin accepts all suggestions.
    /// </summary>
    public string FullEditedReason { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of EditSuggestion objects.
    /// Format: [{ "original": "...", "suggested": "...", "category": "spelling|grammar|name|formatting", "position": 0 }]
    /// </summary>
    public string SuggestionsJson { get; set; } = string.Empty;

    /// <summary>
    /// Which AI model generated these suggestions (e.g., "gpt-4o-mini").
    /// Used to track which model version created each snapshot.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Prompt template version used for generation.
    /// Allows tracking if prompt changes and subsequent regenerations.
    /// </summary>
    public int PromptVersion { get; set; } = 1;

    /// <summary>
    /// When the suggestions were generated.
    /// </summary>
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current status of this suggestion snapshot.
    /// "Success" = ready to use
    /// "Failed" = generation failed, regenerate needed
    /// "Stale" = reason changed, regeneration recommended
    /// </summary>
    public string Status { get; set; } = "Success";

    /// <summary>
    /// If generation failed, stores error message for diagnostics.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Optimistic concurrency token.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}
