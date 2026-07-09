using System.Security.Claims;
using Portal.Domain.Entities;

namespace Portal.Api.Security;

public sealed record ResolvedIdentityClaims(Guid? EntraObjectId, string? Email, string? DisplayName);

public interface IAdminIdentityResolver
{
    ResolvedIdentityClaims ExtractIdentityClaims(ClaimsPrincipal user);
    Task<AdminUser?> ResolveActiveAdminAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<bool> IsActiveAdminAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
