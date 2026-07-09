using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Portal.Application.Features.AdminUsers;
using Portal.Application.Infrastructure.Persistence;
using Portal.Domain.Entities;
using Portal.Domain.Enums;
using System.Security.Claims;
using System.Text.Json;

namespace Portal.Application.Features.AdminUsers.UpdateAdminUser;

public class UpdateAdminUserHandler : IRequestHandler<UpdateAdminUserCommand, AdminUserDto?>
{
    private readonly PortalDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateAdminUserHandler(PortalDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AdminUserDto?> Handle(UpdateAdminUserCommand request, CancellationToken cancellationToken)
    {
        var adminUser = await _dbContext.AdminUsers
            .AsQueryable()
            .FirstOrDefaultAsync(a => a.AdminUserId == request.AdminUserId, cancellationToken);

        if (adminUser == null)
        {
            return null;
        }

        var before = new
        {
            adminUser.CanReview,
            adminUser.ReceiveNotifications,
            adminUser.IsActive,
            adminUser.Department
        };

        adminUser.CanReview = request.CanReview;
        adminUser.ReceiveNotifications = request.ReceiveNotifications;
        adminUser.IsActive = request.IsActive;
        adminUser.Department = request.Department;
        adminUser.UpdatedUtc = DateTime.UtcNow;

        var actor = ResolveActor();

        _dbContext.AuditEvents.Add(new AuditEvent
        {
            AuditEventId = Guid.NewGuid(),
            SubmissionId = null,
            ActorName = actor.ActorName,
            ActorEmail = actor.ActorEmail,
            EventType = AuditEventType.AdminUpdated,
            EventDetail = JsonSerializer.Serialize(new
            {
                action = "admin_updated",
                targetAdmin = new
                {
                    adminUser.AdminUserId,
                    adminUser.EntraObjectId,
                    adminUser.Email,
                    adminUser.DisplayName
                },
                before,
                after = new
                {
                    request.CanReview,
                    request.ReceiveNotifications,
                    request.IsActive,
                    request.Department
                }
            }),
            CreatedUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminUserDto
        {
            AdminUserId = adminUser.AdminUserId,
            EntraObjectId = adminUser.EntraObjectId,
            Email = adminUser.Email,
            DisplayName = adminUser.DisplayName,
            Department = adminUser.Department,
            CanReview = adminUser.CanReview,
            ReceiveNotifications = adminUser.ReceiveNotifications,
            IsActive = adminUser.IsActive,
            CreatedUtc = adminUser.CreatedUtc,
            UpdatedUtc = adminUser.UpdatedUtc
        };
    }

    private (string ActorName, string? ActorEmail) ResolveActor()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var actorName = user?.FindFirst(ClaimTypes.Name)?.Value
            ?? user?.FindFirst("name")?.Value
            ?? "Unknown";
        var actorEmail = user?.FindFirst(ClaimTypes.Email)?.Value
            ?? user?.FindFirst("preferred_username")?.Value
            ?? user?.FindFirst("upn")?.Value;

        return (actorName, actorEmail);
    }
}
