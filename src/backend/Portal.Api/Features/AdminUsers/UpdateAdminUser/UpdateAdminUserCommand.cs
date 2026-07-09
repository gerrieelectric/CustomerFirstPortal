using MediatR;
using Portal.Application.Features.AdminUsers;

namespace Portal.Application.Features.AdminUsers.UpdateAdminUser;

public class UpdateAdminUserCommand : IRequest<AdminUserDto?>
{
    public int AdminUserId { get; set; }
    public bool CanReview { get; set; }
    public bool ReceiveNotifications { get; set; }
    public bool IsActive { get; set; }
    public string? Department { get; set; }

    public static UpdateAdminUserCommand FromRequest(int adminUserId, UpdateAdminUserRequest request)
    {
        return new UpdateAdminUserCommand
        {
            AdminUserId = adminUserId,
            CanReview = request.CanReview,
            ReceiveNotifications = request.ReceiveNotifications,
            IsActive = request.IsActive,
            Department = request.Department
        };
    }
}
