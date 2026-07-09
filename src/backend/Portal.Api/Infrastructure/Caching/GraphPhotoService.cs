using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;

namespace Portal.Application.Infrastructure.Caching;

/// <summary>
/// Fetches user profile photos from Microsoft Graph with built-in caching.
/// </summary>
public class GraphPhotoService : IGraphPhotoService
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPhotoCache _photoCache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GraphPhotoService> _logger;

    public GraphPhotoService(
        ITokenAcquisition tokenAcquisition,
        IHttpClientFactory httpClientFactory,
        IPhotoCache photoCache,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<GraphPhotoService> logger)
    {
        _tokenAcquisition = tokenAcquisition;
        _httpClientFactory = httpClientFactory;
        _photoCache = photoCache;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(byte[] Photo, string ContentType)?> GetUserPhotoAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("GetUserPhotoAsync called with empty userId");
            return null;
        }

        try
        {
            // Create a closure that captures the fetch logic
            var photoBytes = await _photoCache.GetPhotoAsync(
                userId,
                () => FetchPhotoFromGraphAsync(userId, cancellationToken),
                cancellationToken);

            if (photoBytes == null || photoBytes.Length == 0)
            {
                return null;
            }

            // Content type is always JPEG for Microsoft Graph photos
            return (photoBytes, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photo for user {UserId}", userId);
            return null;
        }
    }

    public async Task InvalidateUserPhotoCacheAsync(string userId, CancellationToken cancellationToken = default)
    {
        await _photoCache.InvalidateAsync(userId, cancellationToken);
    }

    private async Task<byte[]?> FetchPhotoFromGraphAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User == null)
            {
                return await FetchPhotoFromGraphWithAppTokenAsync(userId, cancellationToken);
            }

            var scopes = (_configuration["Graph:Scopes"] ?? "User.Read User.ReadBasic.All")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var baseUrl = _configuration["Graph:BaseUrl"] ?? "https://graph.microsoft.com/v1.0";

            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes, user: httpContext.User);
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/users/{userId}/photo/$value");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogDebug("Photo not found or forbidden for user {UserId}", userId);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Graph photo call failed for user {UserId} with status {StatusCode}. Response: {ErrorContent}",
                    userId,
                    (int)response.StatusCode,
                    errorContent);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length == 0)
            {
                _logger.LogDebug("Empty photo response for user {UserId}", userId);
                return null;
            }

            _logger.LogDebug("Successfully fetched photo for user {UserId}, size: {Size} bytes", userId, bytes.Length);
            return bytes;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching photo for user {UserId}", userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching photo for user {UserId}", userId);
            return null;
        }
    }

    private async Task<byte[]?> FetchPhotoFromGraphWithAppTokenAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = _configuration["Entra:TenantId"];
            var clientId = _configuration["Entra:ClientId"];
            var clientSecret = _configuration["Entra:ClientSecret"];

            if (string.IsNullOrWhiteSpace(tenantId) ||
                string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(clientSecret))
            {
                _logger.LogWarning("Graph app-token photo fetch skipped: Entra app credentials are not fully configured");
                return null;
            }

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }),
                cancellationToken);

            var baseUrl = _configuration["Graph:BaseUrl"] ?? "https://graph.microsoft.com/v1.0";
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/users/{userId}/photo/$value");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogDebug("App-token photo not found or forbidden for user {UserId}", userId);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "App-token Graph photo call failed for user {UserId} with status {StatusCode}. Response: {ErrorContent}",
                    userId,
                    (int)response.StatusCode,
                    errorContent);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length == 0)
            {
                _logger.LogDebug("App-token photo response was empty for user {UserId}", userId);
                return null;
            }

            _logger.LogDebug("Successfully fetched photo with app token for user {UserId}, size: {Size} bytes", userId, bytes.Length);
            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "App-token Graph photo fetch failed for user {UserId}", userId);
            return null;
        }
    }
}
