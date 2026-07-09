namespace Portal.Api.Features.Submissions;

public class AttachmentDownloadResponse
{
    public Guid SubmissionAttachmentId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string SasUrl { get; set; } = string.Empty;
    public DateTime ExpiresUtc { get; set; }
}
