namespace Portal.Api.DTOs.Submissions;

/// <summary>
/// Fields extracted from uploaded attachments by the AI extraction service.
/// All fields are null when the value could not be confidently determined.
/// </summary>
public class ExtractedSubmissionFieldsResponse
{
    public string? CompanyName { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? NomineeName { get; set; }
    public string? RecognitionReason { get; set; }

    /// <summary>"Success", "Failed", or "Disabled".</summary>
    public string Status { get; set; } = "Success";
}
