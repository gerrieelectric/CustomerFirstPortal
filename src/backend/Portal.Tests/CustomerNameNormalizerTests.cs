using Portal.Application.Infrastructure.Integrations;

namespace Portal.Tests;

public class CustomerNameNormalizerTests
{
    [Theory]
    [InlineData("Acme Widgets - Cash Sale", "Acme Widgets")]
    [InlineData("Acme Widgets-Cash", "Acme Widgets")]
    [InlineData("Jennifer White - Staff Sale", "Jennifer White")]
    [InlineData("Holiday Retirement-Kingsdale Chateau-Cls", "Holiday Retirement-Kingsdale Chateau")]
    [InlineData("TDSB-John English Jr Middle School-Close", "TDSB-John English Jr Middle School")]
    [InlineData("  Region of  Niagara  ", "Region of Niagara")]
    public void Clean_StripsNoiseMarkers(string input, string expected)
    {
        Assert.Equal(expected, CustomerNameNormalizer.Clean(input));
    }

    [Fact]
    public void Clean_RemovesReplacementCharacters()
    {
        Assert.Equal("SDI c/o Cascades Vaughan", CustomerNameNormalizer.Clean("SDI c/o Cascades \uFFFD Vaughan"));
    }

    [Fact]
    public void NormalizeAndDeduplicate_MergesCaseVariants_PrefersMixedCase()
    {
        var result = CustomerNameNormalizer.NormalizeAndDeduplicate(
            ["MCRAE INTEGRATION LTD", "McRae Integration Ltd"]);

        Assert.Equal(["McRae Integration Ltd"], result);
    }

    [Fact]
    public void NormalizeAndDeduplicate_MergesWhitespaceVariants()
    {
        var result = CustomerNameNormalizer.NormalizeAndDeduplicate(
            ["Black & McDonald Ltd", "Black  & McDonald Ltd"]);

        Assert.Single(result);
        Assert.Equal("Black & McDonald Ltd", result[0]);
    }

    [Fact]
    public void NormalizeAndDeduplicate_MergesLegalSuffixVariants()
    {
        var result = CustomerNameNormalizer.NormalizeAndDeduplicate(
            ["Vac-Aero International", "Vac-Aero International Inc"]);

        Assert.Single(result);
        Assert.Equal("Vac-Aero International Inc", result[0]);
    }

    [Fact]
    public void NormalizeAndDeduplicate_DropsShortAndEmptyNames()
    {
        var result = CustomerNameNormalizer.NormalizeAndDeduplicate(
            ["", "  ", "AB", "Valid Customer Inc"]);

        Assert.Equal(["Valid Customer Inc"], result);
    }

    [Fact]
    public void NormalizeAndDeduplicate_SortsCaseInsensitively()
    {
        var result = CustomerNameNormalizer.NormalizeAndDeduplicate(
            ["zeta Supply", "Alpha Electric", "MIDWAY Controls"]);

        Assert.Equal(["Alpha Electric", "MIDWAY Controls", "zeta Supply"], result);
    }
}
