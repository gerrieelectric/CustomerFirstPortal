namespace Portal.Application.Infrastructure.Integrations;

public interface ICustomerSearchService
{
    Task<IReadOnlyList<string>> SearchCustomersAsync(string query, CancellationToken cancellationToken = default);
}