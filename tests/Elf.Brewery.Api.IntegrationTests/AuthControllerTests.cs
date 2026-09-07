using System.Net;
using System.Net.Http.Json;
using Elf.Brewery.Application.Dtos;
using Xunit;

namespace Elf.Brewery.Api.IntegrationTests;

public class AuthControllerTests : IClassFixture<BreweryApiFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(BreweryApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Token_ValidCredentials_ReturnsOkWithBearerToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/token",
            new LoginRequest("admin@elfbeauty.com", "admin@123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(token);
        Assert.False(string.IsNullOrWhiteSpace(token!.AccessToken));
    }

    [Fact]
    public async Task Token_InvalidCredentials_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/token",
            new LoginRequest("admin@elfbeauty.com", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
