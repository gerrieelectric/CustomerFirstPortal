namespace Portal.Application.Infrastructure.Storage;

public interface IAttachmentStorageService
{
    Task<string> UploadAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken = default);
    Task<string> GetDownloadUrlAsync(string blobName, string downloadFileName, TimeSpan expiresIn, CancellationToken cancellationToken = default);
    Task DeleteAsync(string blobName, CancellationToken cancellationToken = default);
}
