
using System.Collections.Concurrent;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;
using System.Linq;
using Elf.Brewery.Application.Contracts;

namespace Elf.Brewery.Infrastructure.Data;

public sealed class InMemoryBreweryRepository : IBreweryRepository
{

    private readonly ConcurrentDictionary<string, DomainBrewery> _store = new(StringComparer.OrdinalIgnoreCase);
    public Task<IReadOnlyList<DomainBrewery>> GetAllAsync(CancellationToken ct)
    {
        return Task.FromResult((IReadOnlyList<DomainBrewery>)_store.Values.ToList());
    }

    public Task<DomainBrewery?> GetByIdAsync(string id, CancellationToken ct)
    {
        _store.TryGetValue(id, out var brewery);
        return Task.FromResult(brewery);
    }

    public Task<DateTimeOffset?> GetLastRefreshUtcAsync(CancellationToken ct)
    {
        var lastRefresh = _store.Values.Max(x => (DateTimeOffset?)x.LastRefreshedUtc);
        return Task.FromResult(lastRefresh);
    }

    public Task UpsertRangeAsync(IEnumerable<DomainBrewery> breweries, CancellationToken ct)
    {
        foreach (var brewery in breweries)
        {
            _store[brewery.Id] = brewery;
        }
        return Task.CompletedTask;
    }

}