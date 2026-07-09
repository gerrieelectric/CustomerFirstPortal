namespace Portal.Application.Infrastructure.Storage;

public class AzureStorageOptions
{
    public string BlobConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "submissions-attachments-dev";
    public int MaxFileSizeMB { get; set; } = 50;
    public string[] AllowedExtensions { get; set; } = ["pdf", "docx", "png", "jpg", "jpeg", "txt", "eml", "msg"];
}
