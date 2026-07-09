namespace Portal.Api.Features.Submissions;

public class AttachmentUploadResponse
{
    public Guid SubmissionAttachmentId { get; set; }
    public Guid SubmissionId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedUtc { get; set; }
}
