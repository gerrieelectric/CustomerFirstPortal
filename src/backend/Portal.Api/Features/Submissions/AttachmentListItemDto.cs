namespace Portal.Api.Features.Submissions;

public class AttachmentListItemDto
{
    public Guid SubmissionAttachmentId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime UploadedUtc { get; set; }
}
