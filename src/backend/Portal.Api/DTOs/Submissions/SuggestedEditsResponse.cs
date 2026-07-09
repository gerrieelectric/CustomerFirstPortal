namespace Portal.Api.DTOs.Submissions;

/// <summary>
/// Response containing precomputed AI-generated suggestions for a submission's recognition reason.
/// </summary>
public class SuggestedEditsResponse
{
    /// <summary>
    /// The submission ID these suggestions are for.
    /// </summary>
    public Guid SubmissionId { get; set; }

    /// <summary>
    /// The original recognition reason that was processed.
    /// </summary>
    public string OriginalReason { get; set; } = string.Empty;

    /// <summary>
    /// The fully edited recognition reason with all suggestions applied.
    /// Ready to use as-is if admin accepts all suggestions.
    /// </summary>
    public string FullEditedReason { get; set; } = string.Empty;

    /// <summary>
    /// List of individual suggestions (diffs).
    /// Each suggestion shows what to change and why.
    /// </summary>
    public List<EditSuggestion> Suggestions { get; set; } = [];

    /// <summary>
    /// Current status of this suggestion snapshot.
    /// "Success" = ready to use, "Failed" = generation failed, "Stale" = reason changed
    /// </summary>
    public string Status { get; set; } = "Success";

    /// <summary>
    /// When these suggestions were generated.
    /// </summary>
    public DateTime GeneratedUtc { get; set; }

    /// <summary>
    /// Which AI model generated these suggestions (e.g., "gpt-4o-mini").
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Prompt template version used (for tracking).
    /// </summary>
    public int PromptVersion { get; set; }

    /// <summary>
    /// True if status is "Success" and ready to use.
    /// </summary>
    public bool IsReady => Status == "Success" && Suggestions.Count > 0;
}
