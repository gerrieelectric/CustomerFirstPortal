using Portal.Domain.Enums;

namespace Portal.Domain.Entities;

public class GerrieSyncEvent
{
    public Guid GerrieSyncEventId { get; set; }
    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;

    public int AttemptNumber { get; set; }
    public GerrieSyncStatus SyncStatus { get; set; }
    public string? ResponseCode { get; set; }
    public string? ResponseMessage { get; set; }
    public string? ExternalRecordId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
