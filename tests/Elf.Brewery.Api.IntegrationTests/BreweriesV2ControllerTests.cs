using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Elf.Brewery.Application.Dtos;
using Xunit;

namespace Elf.Brewery.Api.IntegrationTests;

/// <summary>
/// Covers the v2 controller, which is backed by the in-memory repository. Mirrors the v1
/// suite so both versions are proven to behave identically for storage-independent logic
/// (filtering, sorting, autocomplete, validation), not just "returns 200/400/404 of some shape".
/// </summary>
public class BreweriesV2ControllerTests : IClassFixture<BreweryApiFactory>
{
    private readonly BreweryApiFactory _factory;

    public BreweriesV2ControllerTests(BreweryApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var tokenResponse = await client.PostAsJsonAsync("/api/auth/token",
            new LoginRequest("admin@elfbeauty.com", "admin@123"));
        var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    private async Task<HttpClient> CreateSeededClientAsync()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Writes the stub data into the in-memory store; every read below comes back out of it.
        await client.PostAsync("/api/v2/breweries/refresh", null);
        return client;
    }

    [Fact]
    public async Task Get_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v2/breweries");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithToken_ReturnsOkWithPagedResult()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v2/breweries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<BreweryDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Refresh_PersistsToInMemoryStore_AndListReadsItBack()
    {
        var client = await CreateSeededClientAsync();

        var result = await client.GetFromJsonAsync<PagedResult<BreweryDto>>("/api/v2/breweries");

        Assert.NotNull(result);
        Assert.Equal(StubBreweryProvider.Breweries.Count, result!.TotalCount);
    }

    [Fact]
    public async Task Get_ReturnsNameCityAndPhone()
    {
        var client = await CreateSeededClientAsync();

        var result = await client.GetFromJsonAsync<PagedResult<BreweryDto>>(
            "/api/v2/breweries?search=Cascade");

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
            "/api/v2/breweries?city=Portland");

        // "Portland Brewing" is in Seattle, so an exact city filter must not return it.
        var brewery = Assert.Single(result!.Items);
        Assert.Equal("Cascade Brewing", brewery.Name);
    }

    [Fact]
    public async Task Get_SearchAndCityCombined_AppliesBothFilters()
    {
        var client = await CreateSeededClientAsync();

        var result = await client.GetFromJsonAsync<PagedResult<BreweryDto>>(
            "/api/v2/breweries?search=Brewing&city=Seattle");

        var brewery = Assert.Single(result!.Items);
        Assert.Equal("Portland Brewing", brewery.Name);
    }

    [Fact]
    public async Task Get_SortByDistance_OrdersNearestFirstAndSkipsMissingCoordinates()
    {
        var client = await CreateSeededClientAsync();

        // Origin is central Portland, so Cascade (also Portland) must come first.
        var result = await client.GetFromJsonAsync<PagedResult<BreweryDto>>(
            "/api/v2/breweries?sortField=Distance&latitude=45.52&longitude=-122.68");

        Assert.Equal("Cascade Brewing", result!.Items[0].Name);
        Assert.NotNull(result.Items[0].DistanceKM);

        // The brewery with null coordinates cannot be ranked, so it drops out.
        Assert.DoesNotContain(result.Items, b => b.Name == "Portland Brewing");
    }

    [Fact]
    public async Task Get_SortByDistanceWithoutCoordinates_ReturnsBadRequest()
    {
        var client = await CreateSeededClientAsync();

        var response = await client.GetAsync("/api/v2/breweries?sortField=Distance");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_PageSizeAboveMaximum_ReturnsBadRequest()
    {
        var client = await CreateSeededClientAsync();

        var response = await client.GetAsync("/api/v2/breweries?pageSize=100000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_KnownId_ReturnsBreweryFromInMemoryStore()
    {
        var client = await CreateSeededClientAsync();

        var brewery = await client.GetFromJsonAsync<BreweryDto>("/api/v2/breweries/test-2");

        Assert.Equal("Alpine Beer Company", brewery!.Name);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v2/breweries/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cities_WithToken_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v2/breweries/cities");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cities = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(cities);
    }

    [Fact]
    public async Task Autocomplete_TermTooShort_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v2/breweries/autocomplete?term=a");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Autocomplete_ReturnsNameSuggestions()
    {
        var client = await CreateSeededClientAsync();

        var items = await client.GetFromJsonAsync<List<AutocompleteItemDto>>(
            "/api/v2/breweries/autocomplete?term=Alp");

        Assert.Single(items!);
        Assert.Equal("Alpine Beer Company", items![0].Name);
    }
}

