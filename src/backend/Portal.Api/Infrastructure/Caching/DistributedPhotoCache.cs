using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Portal.Application.Infrastructure.Caching;

/// <summary>
/// Implements photo caching using IDistributedCache (e.g., Redis).
/// Falls back to local memory caching if distributed cache is unavailable.
/// </summary>
public class DistributedPhotoCache : IPhotoCache
{
    private readonly IDistributedCache? _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly PhotoCacheOptions _options;
    private readonly ILogger<DistributedPhotoCache> _logger;
    private const string CacheKeyPrefix = "photo_";

    public DistributedPhotoCache(
        IDistributedCache? distributedCache,
        IMemoryCache memoryCache,
        IOptions<PhotoCacheOptions> options,
        ILogger<DistributedPhotoCache> logger)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<byte[]?> GetPhotoAsync(
        string userId,
        Func<Task<byte[]?>> fetchPhoto,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return await fetchPhoto();
        }

        var cacheKey = GetCacheKey(userId);

        try
        {
            // Try distributed cache first
            if (_distributedCache != null)
            {
                var cachedData = await _distributedCache.GetAsync(cacheKey, cancellationToken);
                if (cachedData != null && cachedData.Length > 0)
                {
                    _logger.LogDebug("Photo cache hit for user {UserId} from distributed cache", userId);
                    return cachedData;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving photo from distributed cache for user {UserId}", userId);
        }

        // Try memory cache
        if (_memoryCache.TryGetValue(cacheKey, out byte[]? cachedPhoto))
        {
            _logger.LogDebug("Photo cache hit for user {UserId} from memory cache", userId);
            return cachedPhoto;
        }

        // Cache miss - fetch the photo
        _logger.LogDebug("Photo cache miss for user {UserId}, fetching from source", userId);
        var photo = await fetchPhoto();

        if (photo != null && photo.Length > 0)
        {
            await CachePhotoAsync(userId, cacheKey, photo, cancellationToken);
        }

        return photo;
    }

    public async Task InvalidateAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(userId);

        try
        {
            if (_distributedCache != null)
            {
                await _distributedCache.RemoveAsync(cacheKey, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing photo from distributed cache for user {UserId}", userId);
        }

        _memoryCache.Remove(cacheKey);
        _logger.LogInformation("Invalidated photo cache for user {UserId}", userId);
    }

    private async Task CachePhotoAsync(
        string userId,
        string cacheKey,
        byte[] photo,
        CancellationToken cancellationToken)
    {
        var expiration = TimeSpan.FromMinutes(_options.DurationMinutes);

        try
        {
            if (_distributedCache != null)
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                };
                await _distributedCache.SetAsync(cacheKey, photo, cacheOptions, cancellationToken);
                _logger.LogDebug("Cached photo for user {UserId} in distributed cache for {Minutes} minutes", userId, _options.DurationMinutes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error caching photo in distributed cache for user {UserId}", userId);
        }

        // Always cache in memory as fallback
        var memoryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };
        _memoryCache.Set(cacheKey, photo, memoryOptions);
        _logger.LogDebug("Cached photo for user {UserId} in memory cache for {Minutes} minutes", userId, _options.DurationMinutes);
    }

    private static string GetCacheKey(string userId) => $"{CacheKeyPrefix}{userId}";
}
