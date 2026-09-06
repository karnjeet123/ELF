
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class BreweryDataFacade : IBreweryDataFacade
{
    private readonly IBreweryRepository _repository;
    private readonly IBreweryProvider _provider;
    private readonly ICacheService _cache;
    private readonly IOptions<CacheOptions> _cacheOptions;
    private readonly ILogger<BreweryDataFacade> _logger;
    private readonly string _cacheKey;
    private readonly TimeSpan _ttl = default!;
    private static readonly SemaphoreSlim RefreshGate = new(1, 1);
    public BreweryDataFacade([ServiceKey] string storageKey,
        IServiceProvider serviceProvider,
        IBreweryProvider provider,
        ICacheService cache,
        IOptions<CacheOptions> cacheOptions,
        ILogger<BreweryDataFacade> logger)
    {
        _repository = serviceProvider.GetRequiredKeyedService<IBreweryRepository>(storageKey);
        _provider = provider;
        _cache = cache;
        _cacheOptions = cacheOptions;
        _logger = logger;
        _cacheKey = $"breweries:all:{storageKey}";
        _ttl = TimeSpan.FromMinutes(cacheOptions.Value.ExpirationMinutes);
    }

    public async Task<IReadOnlyList<Brewery>> GetAllAsync(CancellationToken ct)
    {
        var (found, cached) = await _cache.TryGetAsync<IReadOnlyList<Brewery>>(_cacheKey, ct);
        if (found && cached is not null)
            return cached;

        await RefreshGate.WaitAsync(ct);
        try
        {
            // Double-check the cache after acquiring the lock
            (found, cached) = await _cache.TryGetAsync<IReadOnlyList<Brewery>>(_cacheKey, ct);
            if (found && cached is not null)
                return cached;
            var data = await _repository.GetAllAsync(ct);
            await _cache.SetAsync(_cacheKey, data, _ttl, ct);
            return data;
        }
        finally
        {
            RefreshGate.Release();
        }

    }

    public async Task<int> RefreshFromExternalAsync(CancellationToken ct)
    {
        _logger.LogInformation("Refreshing breweries from external provider");

        var fetched = await _provider.FetchAllAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var brewery in fetched)
            brewery.LastRefreshedUtc = now;

        await _repository.UpsertRangeAsync(fetched, ct);

        _cache.Remove(_cacheKey);

        _logger.LogInformation("Refreshed {Count} breweries from external provider", fetched.Count);

        return fetched.Count;
    }
}