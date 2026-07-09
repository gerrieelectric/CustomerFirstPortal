namespace Portal.Domain.Enums;

public enum AuditEventType
{
    Submitted,
    AttachmentAdded,
    AttachmentDeleted,
    ReviewOpened,
    Approved,
    Rejected,
    GerrieSyncQueued,
    GerrieSyncSucceeded,
    GerrieSyncFailed,
    AdminUpdated
}
