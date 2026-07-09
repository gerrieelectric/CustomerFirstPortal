using MediatR;

namespace Portal.Application.Features.AdminUsers.RemoveAdminUser;

public class RemoveAdminUserCommand : IRequest<bool>
{
    public int AdminUserId { get; set; }
}
