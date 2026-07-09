using Portal.Domain.Enums;

namespace Portal.Domain.Entities;

public class NotificationDispatch
{
    public Guid NotificationDispatchId { get; set; }
    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;

    public NotificationDispatchType NotificationType { get; set; }
    public NotificationDispatchStatus Status { get; set; }

    public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientDisplayName { get; set; }
    public string? CorrelationId { get; set; }

    public int AttemptCount { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime NextAttemptUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptUtc { get; set; }
    public DateTime? SentUtc { get; set; }
    public string? LastError { get; set; }
}
