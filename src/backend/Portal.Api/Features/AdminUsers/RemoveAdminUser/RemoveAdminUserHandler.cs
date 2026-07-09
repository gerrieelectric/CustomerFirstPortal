using Microsoft.EntityFrameworkCore;
using MediatR;
using Microsoft.AspNetCore.Http;
using Portal.Application.Infrastructure.Persistence;
using Portal.Domain.Entities;
using Portal.Domain.Enums;
using System.Security.Claims;
using System.Text.Json;

namespace Portal.Application.Features.AdminUsers.RemoveAdminUser;

public class RemoveAdminUserHandler : IRequestHandler<RemoveAdminUserCommand, bool>
{
    private readonly PortalDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RemoveAdminUserHandler(PortalDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> Handle(RemoveAdminUserCommand request, CancellationToken cancellationToken)
    {
        var adminUser = await _dbContext.AdminUsers
            .AsQueryable()
            .FirstOrDefaultAsync(a => a.AdminUserId == request.AdminUserId, cancellationToken);

        if (adminUser == null)
        {
            return false;
        }

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
                action = "admin_removed",
                targetAdmin = new
                {
                    adminUser.AdminUserId,
                    adminUser.EntraObjectId,
                    adminUser.Email,
                    adminUser.DisplayName,
                    adminUser.Department,
                    adminUser.CanReview,
                    adminUser.ReceiveNotifications,
                    adminUser.IsActive
                }
            }),
            CreatedUtc = DateTime.UtcNow
        });

        _dbContext.AdminUsers.Remove(adminUser);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
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
