using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace Portal.Application.Infrastructure.Integrations;

public sealed class JdeCustomerSearchService : ICustomerSearchService
{
    private const string CacheKey = "jde-customer-directory-names";
    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<JdeCustomerSearchService> _logger;

    public JdeCustomerSearchService(
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<JdeCustomerSearchService> logger)
    {
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> SearchCustomersAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return [];
        }

        var customers = await GetCustomerNamesAsync(cancellationToken);
        var results = CustomerSearchFilter.FilterCustomers(customers, query, maxResults: 30);
        if (results.Count > 0)
        {
            return results;
        }

        // If no match is found, force a one-time refresh in case the JDE list changed since the cache was populated.
        var refreshedCustomers = await GetCustomerNamesAsync(cancellationToken, forceRefresh: true);
        return CustomerSearchFilter.FilterCustomers(refreshedCustomers, query, maxResults: 30);
    }

    private async Task<IReadOnlyList<string>> GetCustomerNamesAsync(CancellationToken cancellationToken, bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetValue(CacheKey, out IReadOnlyList<string>? cachedCustomers) && cachedCustomers is { Count: > 0 })
        {
            return cachedCustomers;
        }

        await CacheLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && _cache.TryGetValue(CacheKey, out cachedCustomers) && cachedCustomers is { Count: > 0 })
            {
                return cachedCustomers;
            }

            var loadedCustomers = await LoadCustomerNamesAsync(cancellationToken);
            if (loadedCustomers.Count > 0)
            {
                _cache.Set(
                    CacheKey,
                    loadedCustomers,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6),
                        Priority = CacheItemPriority.Normal
                    });
            }

            return loadedCustomers;
        }
        finally
        {
            CacheLock.Release();
        }
    }

    private async Task<IReadOnlyList<string>> LoadCustomerNamesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = _configuration["Gerrie:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogWarning("Gerrie:ConnectionString is missing; customer search will return no results.");
                return [];
            }

            var customers = new List<string>(capacity: 256);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("[dbo].[spJDE_GetCustomersList]", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                CommandTimeout = 45
            };

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                // The stored proc returns a single column aliased [Customer Name].
                var value = reader.IsDBNull(0) ? null : reader.GetValue(0)?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    customers.Add(value.Trim());
                }
            }

            var normalized = customers
                .Where(customer => customer.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(customer => customer)
                .ToList();

            _logger.LogInformation("Loaded {CustomerCount} customer names from JDE for search cache.", normalized.Count);
            return normalized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load customer names from JDE.");
            return [];
        }
    }
}