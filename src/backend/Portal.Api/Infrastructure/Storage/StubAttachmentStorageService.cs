namespace Portal.Application.Infrastructure.Storage;

/// <summary>
/// Stub implementation of IAttachmentStorageService used when Azure Blob Storage is not configured.
/// Allows the application to start but throws if attachment operations are actually used.
/// </summary>
public class StubAttachmentStorageService : IAttachmentStorageService
{
    private readonly string _errorMessage;

    public StubAttachmentStorageService(string errorMessage)
    {
        _errorMessage = errorMessage;
    }

    public async Task<string> UploadAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new InvalidOperationException(_errorMessage);
    }

    public async Task<string> GetDownloadUrlAsync(string blobName, string downloadFileName, TimeSpan expiresIn, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new InvalidOperationException(_errorMessage);
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new InvalidOperationException(_errorMessage);
    }
}
