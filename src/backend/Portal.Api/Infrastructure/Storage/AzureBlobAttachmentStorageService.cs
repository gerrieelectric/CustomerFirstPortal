using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace Portal.Application.Infrastructure.Storage;

public class AzureBlobAttachmentStorageService : IAttachmentStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobAttachmentStorageService(IOptions<AzureStorageOptions> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.BlobConnectionString))
        {
            throw new InvalidOperationException("AzureStorage:BlobConnectionString is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.ContainerName))
        {
            throw new InvalidOperationException("AzureStorage:ContainerName is not configured.");
        }

        _blobServiceClient = new BlobServiceClient(settings.BlobConnectionString);
        _containerClient = _blobServiceClient.GetBlobContainerClient(settings.ContainerName.ToLowerInvariant());
        _containerClient.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<string> UploadAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken = default)
    {
        var safeFileName = Path.GetFileName(originalFileName);
        var extension = Path.GetExtension(safeFileName);
        var blobName = $"submissions/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}{extension}";

        var blobClient = _containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            },
            cancellationToken);

        return blobName;
    }

    public Task<string> GetDownloadUrlAsync(string blobName, string downloadFileName, TimeSpan expiresIn, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);

        // Generate SAS URL if the service client supports it
        if (_blobServiceClient.CanGenerateAccountSasUri)
        {
            var safeFileName = Path.GetFileName(downloadFileName);
            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                safeFileName = Path.GetFileName(blobName);
            }

            // Ensure the browser download name matches the uploaded file name.
            var asciiFileName = safeFileName
                .Replace("\\", string.Empty)
                .Replace("\"", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn),
                ContentDisposition = $"attachment; filename=\"{asciiFileName}\"; filename*=UTF-8''{Uri.EscapeDataString(safeFileName)}"
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return Task.FromResult(sasUri.ToString());
        }

        // Fallback: return blob URI (requires shared access policy or public access)
        return Task.FromResult(blobClient.Uri.ToString());
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
