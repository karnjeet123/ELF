
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;
using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Infrastructure.Contracts;
using Elf.Brewery.Infrastructure.Options;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;

namespace Elf.Brewery.Infrastructure.External.Models;

public sealed class OpenBreweryDbProvider : IBreweryProvider
{
    private readonly HttpClient _httpClient;
    private readonly IBrewerySourceMapper _brewerySourceMapper;
    private readonly IOptions<OpenBreweryDbOptions> _options;
    private readonly ILogger<OpenBreweryDbProvider> _logger;

    public OpenBreweryDbProvider(HttpClient httpClient,
    IBrewerySourceMapper brewerySourceMapper,
    IOptions<OpenBreweryDbOptions> options,
    ILogger<OpenBreweryDbProvider> logger)
    {
        _httpClient = httpClient;
        _brewerySourceMapper = brewerySourceMapper;
        _options = options;
        _logger = logger;
    }
    public async Task<IReadOnlyList<DomainBrewery>> FetchAllAsync(CancellationToken ct)
    {

        var perPage = _options.Value.PerPage;
        var maxPages = _options.Value.MaxPages;
        var allBreweries = new List<DomainBrewery>();
        for (var page = 1; page <= maxPages; page++)
        {
            var response = await _httpClient.GetAsync($"breweries?per_page={perPage}&page={page}", ct);
            response.EnsureSuccessStatusCode();
            var breweries = await response.Content.ReadFromJsonAsync<List<BrewerySourceDto>>(cancellationToken: ct);
            if (breweries == null || breweries.Count == 0)
                break;
            allBreweries.AddRange(breweries.Select(_brewerySourceMapper.ToDomain));
            if (breweries.Count < perPage)
                break;
        }
        _logger.LogInformation("Fetched {Count} breweries from OpenBreweryDb", allBreweries.Count);
        return allBreweries;
    }
}