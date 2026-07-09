using System.Collections.Generic;

namespace Portal.Application.Infrastructure.Integrations;

public static class CustomerSearchFilter
{
    public static IReadOnlyList<string> FilterCustomers(
        IEnumerable<string> customers,
        string query,
        int maxResults = 30)
    {
        if (maxResults <= 0)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return [];
        }

        var normalizedQuery = NormalizeForSearch(query);
        var orderedCustomers = customers
            .Where(customer => !string.IsNullOrWhiteSpace(customer))
            .Select(customer => customer.Trim())
            .Where(customer => customer.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(customer => customer)
            .ToList();

        var normalizedCustomers = orderedCustomers
            .Select(customer => new
            {
                Original = customer,
                Normalized = NormalizeForSearch(customer)
            })
            .ToList();

        var startsWithMatches = normalizedCustomers
            .Where(customer => customer.Normalized.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .Select(customer => customer.Original);

        var containsMatches = normalizedCustomers
            .Where(customer => !customer.Normalized.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase) &&
                               customer.Normalized.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .Select(customer => customer.Original);

        return startsWithMatches
            .Concat(containsMatches)
            .Take(maxResults)
            .ToList();
    }

    private static string NormalizeForSearch(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var result = new char[trimmed.Length];
        var writeIndex = 0;
        var previousWasWhitespace = false;

        foreach (var character in trimmed)
        {
            if (char.IsWhiteSpace(character))
            {
                if (previousWasWhitespace)
                {
                    continue;
                }

                result[writeIndex++] = ' ';
                previousWasWhitespace = true;
                continue;
            }

            result[writeIndex++] = char.ToUpperInvariant(character);
            previousWasWhitespace = false;
        }

        return new string(result, 0, writeIndex);
    }
}