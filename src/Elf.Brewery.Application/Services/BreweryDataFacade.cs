
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Application.Options;

namespace Elf.Brewery.Application.Services;

public class BreweryDataFacade : IBreweryDataFacade
{
    private readonly IBreweryRepository _repository;
    private readonly IBreweryProvider _provider;
    private readonly ICacheService _cache;
    private readonly IOptions<CacheOptions> _cacheOptions;
    private readonly ILogger<BreweryDataFacade> _logger;
    private readonly string _cacheKey;
    private readonly TimeSpan _ttl = default!;
    private readonly TimeSpan _externalRefreshInterval = default!;
    private readonly TimeProvider _timeProvider;
    private static readonly SemaphoreSlim RefreshGate = new(1, 1);
    public BreweryDataFacade([ServiceKey] string storageKey,
        IServiceProvider serviceProvider,
        IBreweryProvider provider,
        ICacheService cache,
        IOptions<CacheOptions> cacheOptions,
        ILogger<BreweryDataFacade> logger,
        TimeProvider timeProvider)
    {
        _repository = serviceProvider.GetRequiredKeyedService<IBreweryRepository>(storageKey);
        _provider = provider;
        _cache = cache;
        _cacheOptions = cacheOptions;
        _logger = logger;
        _timeProvider = timeProvider;
        // Key includes the storage so v1 and v2 never read each other's cached list.
        _cacheKey = $"breweries:all:{storageKey}";
        _ttl = TimeSpan.FromMinutes(cacheOptions.Value.ExpirationMinutes);
        // Independent from _ttl: this bounds how stale the persisted data itself may get
        // before an external call is made, regardless of how aggressively the in-memory
        // cache is expired.
        _externalRefreshInterval = TimeSpan.FromMinutes(cacheOptions.Value.ExternalRefreshMinutes);
    }

    public async Task<IReadOnlyList<DomainBrewery>> GetAllAsync(CancellationToken ct)
    {
        var (found, cached) = await _cache.TryGetAsync<IReadOnlyList<DomainBrewery>>(_cacheKey, ct);
        if (found && cached is not null)
            return cached;
        // Gate stops a cold cache from triggering several parallel repository reads.
        await RefreshGate.WaitAsync(ct);
        try
        {
            // Double-check the cache after acquiring the lock
            (found, cached) = await _cache.TryGetAsync<IReadOnlyList<DomainBrewery>>(_cacheKey, ct);
            if (found && cached is not null)
                return cached;

            if (await IsStaleAsync(ct))
            {
                await RefreshFromExternalAsync(ct);
            }

            var data = await _repository.GetAllAsync(ct);
            await _cache.SetAsync(_cacheKey, data, _ttl, ct);
            return data;
        }
        finally
        {
            RefreshGate.Release();
        }

    }

    /// <summary>
    /// True when the persisted data has never been refreshed, or was last refreshed longer
    /// ago than <c>Cache:ExternalRefreshMinutes</c>. Only this check ever triggers an
    /// external API call from a read; the in-memory cache TTL never does.
    /// </summary>
    private async Task<bool> IsStaleAsync(CancellationToken ct)
    {
        var lastRefreshedUtc = await _repository.GetLastRefreshUtcAsync(ct);
        return lastRefreshedUtc is null
            || _timeProvider.GetUtcNow() - lastRefreshedUtc.Value > _externalRefreshInterval;
    }

    public async Task<int> RefreshFromExternalAsync(CancellationToken ct)
    {
        _logger.LogInformation("Refreshing breweries from external provider");

        var fetched = await _provider.FetchAllAsync(ct);

        var now = _timeProvider.GetUtcNow();
        foreach (var brewery in fetched)
            brewery.LastRefreshedUtc = now;

        await _repository.UpsertRangeAsync(fetched, ct);

        _cache.Remove(_cacheKey);

        _logger.LogInformation("Refreshed {Count} breweries from external provider", fetched.Count);

        return fetched.Count;
    }
}