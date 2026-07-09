using Portal.Application.Infrastructure.Integrations;

namespace Portal.Tests;

public class CustomerSearchFilterTests
{
    [Fact]
    public void FilterCustomers_ReturnsEmpty_WhenQueryTooShort()
    {
        var result = CustomerSearchFilter.FilterCustomers(
            ["Acme", "Contoso"],
            "a",
            30);

        Assert.Empty(result);
    }

    [Fact]
    public void FilterCustomers_Prioritizes_StartsWith_BeforeContains()
    {
        var result = CustomerSearchFilter.FilterCustomers(
            ["Mega Acme Holdings", "Acme Corp", "Contoso"],
            "ac",
            30);

        Assert.Equal(["Acme Corp", "Mega Acme Holdings"], result);
    }

    [Fact]
    public void FilterCustomers_IsCaseInsensitive_AndCapsResults()
    {
        var customers = Enumerable.Range(1, 50)
            .Select(index => index == 1 ? "Northwind Traders" : $"Northwind Customer {index}")
            .ToList();

        var result = CustomerSearchFilter.FilterCustomers(customers, "NORTH", 30);

        Assert.Equal(30, result.Count);
        Assert.All(result, customer => Assert.Contains("north", customer, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FilterCustomers_Matches_WhenCustomerHasExtraWhitespace()
    {
        var result = CustomerSearchFilter.FilterCustomers(
            ["PAYNEL  ELECTRICAL   CONTRACTORS"],
            "paynel electrical contractors",
            30);

        Assert.Single(result);
        Assert.Equal("PAYNEL  ELECTRICAL   CONTRACTORS", result[0]);
    }
}