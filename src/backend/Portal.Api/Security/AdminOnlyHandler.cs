using Microsoft.AspNetCore.Authorization;

namespace Portal.Api.Security;

public sealed class AdminOnlyHandler(
    IAdminIdentityResolver adminIdentityResolver,
    ILogger<AdminOnlyHandler> logger)
    : AuthorizationHandler<AdminOnlyRequirement>
{
    private readonly IAdminIdentityResolver _adminIdentityResolver = adminIdentityResolver;
    private readonly ILogger<AdminOnlyHandler> _logger = logger;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminOnlyRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var isActiveAdmin = await _adminIdentityResolver.IsActiveAdminAsync(context.User);
        if (isActiveAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        var identityClaims = _adminIdentityResolver.ExtractIdentityClaims(context.User);
        _logger.LogWarning(
            "AdminOnly check failed. ObjectId: {ObjectId}, Email: {Email}",
            identityClaims.EntraObjectId,
            identityClaims.Email ?? "(missing)");
    }
}
