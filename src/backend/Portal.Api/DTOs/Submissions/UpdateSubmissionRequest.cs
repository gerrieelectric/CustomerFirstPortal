namespace Portal.Application.DTOs.Submissions;

public class UpdateSubmissionRequest
{
    public string? CompanyName { get; set; }
    public string? NomineeName { get; set; }
    public string? NomineeEmail { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }

    public string? RecognitionReason { get; set; }

    public int? ServiceRating { get; set; }
}
