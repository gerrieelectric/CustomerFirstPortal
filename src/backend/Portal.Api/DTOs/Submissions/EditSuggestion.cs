namespace Portal.Api.DTOs.Submissions;

/// <summary>
/// Represents a single suggestion for editing the recognition reason.
/// </summary>
public class EditSuggestion
{
    /// <summary>
    /// The original text that should be changed.
    /// </summary>
    public string Original { get; set; } = string.Empty;

    /// <summary>
    /// The suggested replacement text.
    /// </summary>
    public string Suggested { get; set; } = string.Empty;

    /// <summary>
    /// Category of the suggestion: "spelling", "grammar", "name", or "formatting".
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Character position in the original reason where this change applies.
    /// Used for highlighting/display purposes.
    /// </summary>
    public int Position { get; set; }
}
