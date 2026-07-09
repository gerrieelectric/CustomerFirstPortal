using Portal.Domain.Enums;

namespace Portal.Domain.Entities;

public class Submission
{
    public Guid SubmissionId { get; set; }
    public SubmissionType SubmissionType { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    // Submitter
    public string SubmittedByName { get; set; } = string.Empty;
    public string SubmittedByEmail { get; set; } = string.Empty;
    public string? SubmittedByDepartment { get; set; }
    public Guid? SubmittedByEntraObjectId { get; set; }

    // Nominee
    public string NomineeName { get; set; } = string.Empty;
    public string? NomineeEmail { get; set; }
    public string? NomineeDepartment { get; set; }

    // Customer (only for EmployeeOnBehalfOfCustomer / CustomerToEmployee)
    public string? CompanyName { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }

    // Recognition
    public string RecognitionReason { get; set; } = string.Empty;
    public byte? ServiceRating { get; set; }

    // Timestamps
    public DateTime SubmittedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedUtc { get; set; }
    public DateTime? RejectedUtc { get; set; }

    // Review
    public int? ReviewedByAdminUserId { get; set; }
    public AdminUser? ReviewedByAdmin { get; set; }

    // Gerrie integration
    public string? GerrieRecordId { get; set; }
    public DateTime? LastGerrieSyncUtc { get; set; }

    // Optimistic concurrency
    public byte[] RowVersion { get; set; } = [];

    // Navigation
    public ICollection<SubmissionAttachment> Attachments { get; set; } = [];
    public ICollection<SubmissionReview> Reviews { get; set; } = [];
    public ICollection<SubmissionSuggestion> Suggestions { get; set; } = [];
    public ICollection<GerrieSyncEvent> GerrieSyncEvents { get; set; } = [];
    public ICollection<AuditEvent> AuditEvents { get; set; } = [];

    // Domain helpers
    public bool CanBeReviewed() =>
        Status is SubmissionStatus.Submitted;

    public bool IsApproved() =>
        Status is SubmissionStatus.Approved;

    public void MarkApproved(int adminUserId)
    {
        Status = SubmissionStatus.Approved;
        ApprovedUtc = DateTime.UtcNow;
        ReviewedByAdminUserId = adminUserId;
    }

    public void MarkRejected(int adminUserId)
    {
        Status = SubmissionStatus.Rejected;
        RejectedUtc = DateTime.UtcNow;
        ReviewedByAdminUserId = adminUserId;
    }

    public void MarkGerrieSynced(string externalRecordId)
    {
        // Keep synced submissions in Approved to avoid surfacing a legacy status.
        Status = SubmissionStatus.Approved;
        GerrieRecordId = externalRecordId;
        LastGerrieSyncUtc = DateTime.UtcNow;
    }

    public void MarkGerrieSyncFailed()
    {
        Status = SubmissionStatus.GerrieSyncFailed;
        LastGerrieSyncUtc = DateTime.UtcNow;
    }
}
