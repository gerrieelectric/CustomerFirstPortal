namespace Portal.Application.Infrastructure.Caching;

/// <summary>
/// Provides access to user profile photos from Microsoft Graph with caching support.
/// </summary>
public interface IGraphPhotoService
{
    /// <summary>
    /// Gets the profile photo for the specified user ID.
    /// Results are cached according to PhotoCacheOptions.
    /// </summary>
    Task<(byte[] Photo, string ContentType)?> GetUserPhotoAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cached photo for the specified user ID.
    /// </summary>
    Task InvalidateUserPhotoCacheAsync(string userId, CancellationToken cancellationToken = default);
}
