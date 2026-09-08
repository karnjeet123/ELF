using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Elf.Brewery.Application.Dtos;
using Xunit;

namespace Elf.Brewery.Api.IntegrationTests;

/// <summary>
/// Covers the v1 controller, which is backed by the Sqlite repository. Data is seeded
/// through the real refresh endpoint (against a stubbed provider), so these tests prove
/// the database is genuinely written to and read back from.
/// </summary>
public class BreweriesV1ControllerTests : IClassFixture<BreweryApiFactory>
{
    private readonly BreweryApiFactory _factory;

    public BreweriesV1ControllerTests(BreweryApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateSeededClientAsync()
    {
        var client = _factory.CreateClient();
        var tokenResponse = await client.PostAsJsonAsync("/api/auth/token",
            new LoginRequest("admin@elfbeauty.com", "admin@123"));
        var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        // Writes the stub data into Sqlite; every read below comes back out of the database.
        await client.PostAsync("/api/v1/breweries/refresh", null);
        return client;
    }

    [Fact]
    public async Task Get_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/breweries");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_PersistsToSqlite_AndListReadsItBack()
    {
        var client = await CreateSeededClientAsync();

        var result = await client.GetFromJsonAsync<PagedResult<BreweryDto>>("/api/v1/breweries");

        Assert.NotNull(result);
        Assert.Equal(StubBreweryProvider.Breweries.Count, result!.TotalCount);
    }

    [Fact]
    public async Task Get_ReturnsNameCityAndPhone()
    {
        var client = await CreateSeededClientAsync();

        var result = await client.GetFromJsonAsync<PagedResult<BreweryDto>>(
            "/api/v1/breweries?search=Cascade");

        var brewery = Assert.Single(result!.Items);
        Assert.Equal("Cascade Brewing", brewery.Name);
        Assert.Equal("Portland", brewery.City);
        Assert.Equal("(503) 265-8603", brewery.Phone);
    }

    [Fact]
    public async Task Get_CityFilter_MatchesExactlyAndExcludesNameMatches()
    {
        var client = await CreateSeededClientAsync();

        var result = await client.GetFromJsonAsync<PagedResult<BreweryDto>>(
            "/api/v1/breweries?city=Portland");

        // "Portland Brewing" is in Seattle, so an exact city filter must not return it.
        var brewery = Assert.Single(result!.Items);
        Assert.Equal("Cascade Brewing", brewery.Name);
    }

    [Fact]
    public async Task Get_SearchAndCityCombined_AppliesBothFilters()
    {
        var client = await CreateSeededClientAsync();

        var result = await client.GetFromJsonAsync<PagedResult<BreweryDto>>(
            "/api/v1/breweries?search=Brewing&city=Seattle");

        var brewery = Assert.Single(result!.Items);
        Assert.Equal("Portland Brewing", brewery.Name);
    }

    [Fact]
    public async Task Get_SortByDistance_OrdersNearestFirstAndSkipsMissingCoordinates()
    {
        var client = await CreateSeededClientAsync();

        // Origin is central Portland, so Cascade (also Portland) must come first.
        var result = await client.GetFromJsonAsync<PagedResult<BreweryDto>>(
            "/api/v1/breweries?sortField=Distance&latitude=45.52&longitude=-122.68");

        Assert.Equal("Cascade Brewing", result!.Items[0].Name);
        Assert.NotNull(result.Items[0].DistanceKM);

        // The brewery with null coordinates cannot be ranked, so it drops out.
        Assert.DoesNotContain(result.Items, b => b.Name == "Portland Brewing");
    }

    [Fact]
    public async Task Get_SortByDistanceWithoutCoordinates_ReturnsBadRequest()
    {
        var client = await CreateSeededClientAsync();

        var response = await client.GetAsync("/api/v1/breweries?sortField=Distance");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_PageSizeAboveMaximum_ReturnsBadRequest()
    {
        var client = await CreateSeededClientAsync();

        var response = await client.GetAsync("/api/v1/breweries?pageSize=100000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_KnownId_ReturnsBreweryFromDatabase()
    {
        var client = await CreateSeededClientAsync();

        var brewery = await client.GetFromJsonAsync<BreweryDto>("/api/v1/breweries/test-2");

        Assert.Equal("Alpine Beer Company", brewery!.Name);
    }

    [Fact]
    public async Task Autocomplete_ReturnsNameSuggestions()
    {
        var client = await CreateSeededClientAsync();

        var items = await client.GetFromJsonAsync<List<AutocompleteItemDto>>(
            "/api/v1/breweries/autocomplete?term=Alp");

        Assert.Single(items!);
        Assert.Equal("Alpine Beer Company", items![0].Name);
    }
}
