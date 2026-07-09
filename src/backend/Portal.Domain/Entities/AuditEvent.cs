using Portal.Domain.Enums;

namespace Portal.Domain.Entities;

public class AuditEvent
{
    public Guid AuditEventId { get; set; }
    public Guid? SubmissionId { get; set; }
    public Submission? Submission { get; set; }

    public string ActorName { get; set; } = string.Empty;
    public string? ActorEmail { get; set; }
    public AuditEventType EventType { get; set; }
    /// <summary>Optional JSON payload with contextual detail (e.g. old vs new field values).</summary>
    public string? EventDetail { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
