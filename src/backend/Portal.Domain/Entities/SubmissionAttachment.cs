namespace Portal.Domain.Entities;

public class SubmissionAttachment
{
    public Guid SubmissionAttachmentId { get; set; }
    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;

    public string OriginalFileName { get; set; } = string.Empty;
    /// <summary>Name of the blob in Azure Blob Storage.</summary>
    public string BlobName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;
}
