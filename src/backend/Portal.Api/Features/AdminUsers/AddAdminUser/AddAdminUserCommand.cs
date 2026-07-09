using MediatR;
using Portal.Application.Features.AdminUsers;

namespace Portal.Application.Features.AdminUsers.AddAdminUser;

public class AddAdminUserCommand : IRequest<AdminUserDto>
{
    public Guid EntraObjectId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public bool CanReview { get; set; } = true;
    public bool ReceiveNotifications { get; set; } = true;

    public static AddAdminUserCommand FromRequest(AddAdminUserRequest request)
    {
        return new AddAdminUserCommand
        {
            EntraObjectId = request.EntraObjectId,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Department = request.Department,
            CanReview = request.CanReview,
            ReceiveNotifications = request.ReceiveNotifications
        };
    }
}
