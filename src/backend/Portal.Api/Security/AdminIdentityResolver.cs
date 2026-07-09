using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Portal.Application.Infrastructure.Persistence;
using Portal.Domain.Entities;

namespace Portal.Api.Security;

public sealed class AdminIdentityResolver(
    PortalDbContext dbContext,
    ILogger<AdminIdentityResolver> logger,
    IMemoryCache cache,
    IAdminIdentityCacheEpoch cacheEpoch,
    IConfiguration configuration) : IAdminIdentityResolver
{
    private sealed record CachedAdminLookup(AdminUser? AdminUser);

    private readonly PortalDbContext _dbContext = dbContext;
    private readonly ILogger<AdminIdentityResolver> _logger = logger;
    private readonly IMemoryCache _cache = cache;
    private readonly IAdminIdentityCacheEpoch _cacheEpoch = cacheEpoch;
    private readonly IConfiguration _configuration = configuration;

    public ResolvedIdentityClaims ExtractIdentityClaims(ClaimsPrincipal user)
    {
        var objectIdValue = user.FindFirst("oid")?.Value
            ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? user.FindFirst("objectidentifier")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var emailValue = user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? user.FindFirst("upn")?.Value;

        var displayName = user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst("name")?.Value;

        Guid? entraObjectId = null;
        if (!string.IsNullOrWhiteSpace(objectIdValue))
        {
            if (Guid.TryParse(objectIdValue, out var parsedObjectId))
            {
                entraObjectId = parsedObjectId;
            }
            else
            {
                _logger.LogDebug("Unable to parse admin object ID claim value: {ObjectIdValue}", objectIdValue);
            }
        }

        return new ResolvedIdentityClaims(
            EntraObjectId: entraObjectId,
            Email: NormalizeEmail(emailValue),
            DisplayName: string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim());
    }

    public async Task<AdminUser?> ResolveActiveAdminAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var claims = ExtractIdentityClaims(user);
        var epoch = _cacheEpoch.GetCurrentEpoch();
        var activeAdminTtlSeconds = Math.Max(5, _configuration.GetValue<int?>("Security:AdminIdentityCacheSeconds") ?? 30);
        var missingAdminTtlSeconds = Math.Max(2, _configuration.GetValue<int?>("Security:AdminIdentityMissingCacheSeconds") ?? 10);

        if (claims.EntraObjectId.HasValue)
        {
            var objectIdCacheKey = $"admin-identity:{epoch}:oid:{claims.EntraObjectId.Value}";
            var adminByObjectIdLookup = await _cache.GetOrCreateAsync(
                objectIdCacheKey,
                async cacheEntry =>
                {
                    var adminByObjectId = await _dbContext.AdminUsers
                        .AsNoTracking()
                        .Where(admin => admin.IsActive)
                        .FirstOrDefaultAsync(admin => admin.EntraObjectId == claims.EntraObjectId.Value, cancellationToken);

                    cacheEntry.AbsoluteExpirationRelativeToNow = adminByObjectId != null
                        ? TimeSpan.FromSeconds(activeAdminTtlSeconds)
                        : TimeSpan.FromSeconds(missingAdminTtlSeconds);

                    return new CachedAdminLookup(adminByObjectId);
                });

            var adminByObjectId = adminByObjectIdLookup?.AdminUser;

            if (adminByObjectId != null)
            {
                return adminByObjectId;
            }
        }

        if (!string.IsNullOrWhiteSpace(claims.Email))
        {
            var emailCacheKey = $"admin-identity:{epoch}:email:{claims.Email}";
            var adminByEmailLookup = await _cache.GetOrCreateAsync(
                emailCacheKey,
                async cacheEntry =>
                {
                    var adminByEmail = await _dbContext.AdminUsers
                        .AsNoTracking()
                        .Where(admin => admin.IsActive)
                        .FirstOrDefaultAsync(admin => admin.Email.ToLower() == claims.Email, cancellationToken);

                    cacheEntry.AbsoluteExpirationRelativeToNow = adminByEmail != null
                        ? TimeSpan.FromSeconds(activeAdminTtlSeconds)
                        : TimeSpan.FromSeconds(missingAdminTtlSeconds);

                    return new CachedAdminLookup(adminByEmail);
                });

            var adminByEmail = adminByEmailLookup?.AdminUser;

            if (adminByEmail != null)
            {
                return adminByEmail;
            }
        }

        return null;
    }

    public async Task<bool> IsActiveAdminAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var adminUser = await ResolveActiveAdminAsync(user, cancellationToken);
        return adminUser != null;
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return email.Trim().ToLowerInvariant();
    }
}
