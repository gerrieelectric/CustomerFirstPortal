namespace Portal.Application.DTOs.Submissions;

public class CreateSubmissionRequest
{
    public string SubmissionType { get; set; } = null!;

    public string? SubmittedByName { get; set; }
    public string? SubmittedByEmail { get; set; }
    public string? SubmittedByDepartment { get; set; }
    public Guid? SubmittedByEntraObjectId { get; set; }

    public string NomineeName { get; set; } = null!;
    public string NomineeEmail { get; set; } = null!;
    public string? NomineeDepartment { get; set; }

    public string? CompanyName { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }

    public string RecognitionReason { get; set; } = null!;

    public int? ServiceRating { get; set; }
}
