using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Features.AdminUsers;
using Portal.Application.Infrastructure.Persistence;

namespace Portal.Application.Features.AdminUsers.ListAdminUsers;

public class ListAdminUsersQuery : IRequest<List<AdminUserDto>>
{
    public bool? IsActive { get; set; }
}

public class ListAdminUsersHandler : IRequestHandler<ListAdminUsersQuery, List<AdminUserDto>>
{
    private readonly PortalDbContext _dbContext;

    public ListAdminUsersHandler(PortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AdminUserDto>> Handle(ListAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.AdminUsers.AsQueryable();

        if (request.IsActive.HasValue)
        {
            query = query.Where(a => a.IsActive == request.IsActive.Value);
        }

        var adminUsers = await query
            .OrderBy(a => a.DisplayName)
            .ToListAsync(cancellationToken);

        return adminUsers.Select(a => new AdminUserDto
        {
            AdminUserId = a.AdminUserId,
            EntraObjectId = a.EntraObjectId,
            Email = a.Email,
            DisplayName = a.DisplayName,
            Department = a.Department,
            CanReview = a.CanReview,
            ReceiveNotifications = a.ReceiveNotifications,
            IsActive = a.IsActive,
            CreatedUtc = a.CreatedUtc,
            UpdatedUtc = a.UpdatedUtc
        }).ToList();
    }
}
