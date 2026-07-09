namespace Portal.Application.Infrastructure.Caching;

/// <summary>
/// Provides caching for employee photos from Microsoft Graph.
/// </summary>
public interface IPhotoCache
{
    /// <summary>
    /// Gets a cached photo for the specified user ID, or fetches it if not cached.
    /// </summary>
    Task<byte[]?> GetPhotoAsync(string userId, Func<Task<byte[]?>> fetchPhoto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cached photo for the specified user ID.
    /// </summary>
    Task InvalidateAsync(string userId, CancellationToken cancellationToken = default);
}
