namespace Portal.Application.DTOs.Submissions;

public class SubmissionListItemDto
{
    public Guid SubmissionId { get; set; }

    public string SubmissionType { get; set; } = null!;
    public string Status { get; set; } = null!;

    public string SubmittedByName { get; set; } = null!;
    public string SubmittedByEmail { get; set; } = null!;

    public string? CompanyName { get; set; }
    public string NomineeName { get; set; } = null!;

    public string RecognitionReason { get; set; } = null!;

    public DateTime SubmittedUtc { get; set; }
    public int? ReviewedByAdminUserId { get; set; }

    public string? GerrieRecordId { get; set; }
}
