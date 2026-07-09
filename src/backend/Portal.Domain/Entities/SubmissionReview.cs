using Portal.Domain.Enums;

namespace Portal.Domain.Entities;

public class SubmissionReview
{
    public Guid SubmissionReviewId { get; set; }
    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;

    public int AdminUserId { get; set; }
    public AdminUser AdminUser { get; set; } = null!;

    public ReviewDecision Decision { get; set; }
    public string? ReviewNotes { get; set; }
    /// <summary>If the admin edited the recognition text before approving, the edited version is stored here.</summary>
    public string? EditedRecognitionReason { get; set; }
    public DateTime ReviewedUtc { get; set; } = DateTime.UtcNow;
}
