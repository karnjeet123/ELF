using System.Net;
using Xunit;

namespace Elf.Brewery.Api.IntegrationTests;

public class HealthCheckTests : IClassFixture<BreweryApiFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(BreweryApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", body);
    }
}
