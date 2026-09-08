
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;
using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Domain.Exceptions;
using Elf.Brewery.Infrastructure.Contracts;
using Elf.Brewery.Infrastructure.External.Models;
using Elf.Brewery.Infrastructure.Options;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;

namespace Elf.Brewery.Infrastructure.External;

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
        var reachedEnd = false;
        for (var page = 1; page <= maxPages; page++)
        {
            List<BrewerySourceDto>? breweries;
            try
            {
                var response = await _httpClient.GetAsync($"breweries?per_page={perPage}&page={page}", ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "OpenBreweryDb returned {StatusCode} for page {Page}",
                        (int)response.StatusCode, page);

                    throw new ExternalServiceException(
                        $"Open Brewery DB returned {(int)response.StatusCode} ({response.ReasonPhrase}).");
                }

                breweries = await response.Content.ReadFromJsonAsync<List<BrewerySourceDto>>(cancellationToken: ct);
            }
            catch (HttpRequestException ex)
            {
                // Network failure, DNS problem, or Polly's circuit breaker being open.
                _logger.LogError(ex, "Could not reach OpenBreweryDb on page {Page}", page);
                throw new ExternalServiceException("Could not reach Open Brewery DB.", ex);
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                // Cancellation the caller did not ask for means the HttpClient timeout elapsed.
                _logger.LogError(ex, "OpenBreweryDb request timed out on page {Page}", page);
                throw new ExternalServiceException("Open Brewery DB timed out.", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "OpenBreweryDb returned malformed JSON on page {Page}", page);
                throw new ExternalServiceException("Open Brewery DB returned malformed data.", ex);
            }

            if (breweries == null || breweries.Count == 0)
            {
                reachedEnd = true;
                break;
            }
            allBreweries.AddRange(breweries.Select(_brewerySourceMapper.ToDomain));
            if (breweries.Count < perPage)
            {
                reachedEnd = true;
                break;
            }
        }

        if (!reachedEnd)
        {
            _logger.LogWarning(
                "Stopped fetching breweries after reaching MaxPages={MaxPages} (per_page={PerPage}); more data may exist upstream and was not fetched. Fetched {Count} so far.",
                maxPages, perPage, allBreweries.Count);
        }

        _logger.LogInformation("Fetched {Count} breweries from OpenBreweryDb", allBreweries.Count);
        return allBreweries;
    }
}
