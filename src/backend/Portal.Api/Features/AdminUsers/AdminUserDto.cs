namespace Portal.Application.Features.AdminUsers;

public class AdminUserDto
{
    public int AdminUserId { get; set; }
    public Guid EntraObjectId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public bool CanReview { get; set; }
    public bool ReceiveNotifications { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class AddAdminUserRequest
{
    public Guid EntraObjectId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public bool CanReview { get; set; } = true;
    public bool ReceiveNotifications { get; set; } = true;
}

public class UpdateAdminUserRequest
{
    public bool CanReview { get; set; }
    public bool ReceiveNotifications { get; set; }
    public bool IsActive { get; set; }
    public string? Department { get; set; }
}
