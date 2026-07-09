using Portal.Api.Features.Submissions;

namespace Portal.Application.DTOs.Submissions;

public class SubmissionResponse
{
    public Guid SubmissionId { get; set; }

    public string SubmissionType { get; set; } = null!;
    public string Status { get; set; } = null!;

    public string SubmittedByName { get; set; } = null!;
    public string SubmittedByEmail { get; set; } = null!;
    public string? SubmittedByDepartment { get; set; }

    public string NomineeName { get; set; } = null!;
    public string? NomineeEmail { get; set; }
    public string? NomineeDepartment { get; set; }

    public string? CompanyName { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }

    public string RecognitionReason { get; set; } = null!;

    public byte? ServiceRating { get; set; }

    public DateTime SubmittedUtc { get; set; }
    public DateTime? ApprovedUtc { get; set; }
    public DateTime? RejectedUtc { get; set; }
    public string RowVersion { get; set; } = null!;

    public int? ReviewedByAdminUserId { get; set; }

    public string? GerrieRecordId { get; set; }
    public DateTime? LastGerrieSyncUtc { get; set; }

    public List<AttachmentListItemDto> Attachments { get; set; } = new();
}
