namespace Portal.Application.Features.AdminUsers;

public class DirectoryUserDto
{
    public string EntraObjectId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}
