using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Elf.Brewery.Application.Dtos;
using Xunit;

namespace Elf.Brewery.Api.IntegrationTests;

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
}
