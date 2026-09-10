using Elf.Brewery.Application.Search;
using Xunit;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;

namespace Elf.Brewery.Application.Tests.Search;

public class BrewerySearchServiceTests
{
    private static DomainBrewery Make(string id, string name, string? city = null) =>
        new() { Id = id, Name = name, City = city };

    [Fact]
    public void Suggest_DuplicateNames_ReturnsEachNameOnce()
    {
        var sut = new BrewerySearchService();
        var source = new[]
        {
            Make("1", "Cascade Brewing", "Portland"),
            Make("2", "Cascade Brewing", "Seattle"),
            Make("3", "Cascade Lakes Brewing", "Redmond"),
        };

        var result = sut.Suggest(source, "cascade", limit: 10);

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { "Cascade Brewing", "Cascade Lakes Brewing" }, result.Select(r => r.Name));
    }

    [Fact]
    public void Suggest_DuplicateNames_KeepsBestRankedMatch()
    {
        var sut = new BrewerySearchService();
        var source = new[]
        {
            Make("1", "Cascade Brewing", "Seattle"),
            Make("2", "Cascade Brewing", "Portland"),
        };

        var result = sut.Suggest(source, "cascade", limit: 10);

        Assert.Single(result);
        Assert.Equal("Seattle", result[0].City);
    }

    [Fact]
    public void Suggest_NoMatchingTerm_ReturnsEmpty()
    {
        var sut = new BrewerySearchService();
        var source = new[] { Make("1", "Cascade Brewing") };

        var result = sut.Suggest(source, "porter", limit: 10);

        Assert.Empty(result);
    }

    [Fact]
    public void Suggest_BlankTerm_ReturnsEmpty()
    {
        var sut = new BrewerySearchService();
        var source = new[] { Make("1", "Cascade Brewing") };

        var result = sut.Suggest(source, "  ", limit: 10);

        Assert.Empty(result);
    }
}
