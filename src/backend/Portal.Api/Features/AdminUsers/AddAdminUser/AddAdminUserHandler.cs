using MediatR;
using Microsoft.AspNetCore.Http;
using Portal.Application.Features.AdminUsers;
using Portal.Application.Infrastructure.Persistence;
using Portal.Domain.Entities;
using Portal.Domain.Enums;
using System.Security.Claims;
using System.Text.Json;

namespace Portal.Application.Features.AdminUsers.AddAdminUser;

public class AddAdminUserHandler : IRequestHandler<AddAdminUserCommand, AdminUserDto>
{
    private readonly PortalDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AddAdminUserHandler(PortalDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AdminUserDto> Handle(AddAdminUserCommand request, CancellationToken cancellationToken)
    {
        var adminUser = new AdminUser
        {
            EntraObjectId = request.EntraObjectId,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Department = request.Department,
            CanReview = request.CanReview,
            ReceiveNotifications = request.ReceiveNotifications,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        var actor = ResolveActor();

        _dbContext.AdminUsers.Add(adminUser);
        _dbContext.AuditEvents.Add(new AuditEvent
        {
            AuditEventId = Guid.NewGuid(),
            SubmissionId = null,
            ActorName = actor.ActorName,
            ActorEmail = actor.ActorEmail,
            EventType = AuditEventType.AdminUpdated,
            EventDetail = JsonSerializer.Serialize(new
            {
                action = "admin_added",
                targetAdmin = new
                {
                    request.EntraObjectId,
                    request.Email,
                    request.DisplayName,
                    request.Department,
                    request.CanReview,
                    request.ReceiveNotifications
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
