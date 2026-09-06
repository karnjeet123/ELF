
using System.Collections.Concurrent;

public sealed class InMemoryBreweryRepository : IBreweryRepository
{
    
    private readonly ConcurrentDictionary<string, Brewery> _store = new(StringComparer.OrdinalIgnoreCase); 
    public Task<IReadOnlyList<Brewery>> GetAllAsync(CancellationToken ct)
    {
        return Task.FromResult((IReadOnlyList<Brewery>)_store.Values.ToList());
    }

    public Task<Brewery?> GetByIdAsync(string id, CancellationToken ct)
    {
        _store.TryGetValue(id, out var brewery);
        return Task.FromResult(brewery);
    }

    public Task<DateTimeOffset?> GetLastRefreshUtcAsync(CancellationToken ct)
    {
        var lastRefresh = _store.Values.Max(x => (DateTimeOffset?)x.LastRefreshedUtc);
        return Task.FromResult(lastRefresh);
    }

    public Task UpsertRangeAsync(IEnumerable<Brewery> breweries, CancellationToken ct)
    {
        foreach (var brewery in breweries)
        {
            _store[brewery.Id] = brewery;
        }
        return Task.CompletedTask;
    }
}