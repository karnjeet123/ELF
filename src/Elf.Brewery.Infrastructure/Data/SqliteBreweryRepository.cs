using Microsoft.EntityFrameworkCore;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;
using Elf.Brewery.Application.Contracts;

namespace Elf.Brewery.Infrastructure.Data;

public sealed class SqliteBreweryRepository : IBreweryRepository
{
    private readonly BreweryDbContext _db;

    public SqliteBreweryRepository(BreweryDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DomainBrewery>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Breweries.AsNoTracking().ToListAsync(ct);
    }

    public Task<DomainBrewery?> GetByIdAsync(string id, CancellationToken ct)
        => _db.Breweries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task UpsertRangeAsync(IEnumerable<DomainBrewery> items, CancellationToken ct)
    {
        var incoming = items.ToList();
        var ids = incoming.Select(x => x.Id).ToHashSet();

        var existing = await _db.Breweries
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        foreach (var item in incoming)
        {
            if (existing.TryGetValue(item.Id, out var current))
                _db.Entry(current).CurrentValues.SetValues(item);
            else
                _db.Breweries.Add(item);
        }

        await _db.SaveChangesAsync(ct);
    }

    public Task<DateTimeOffset?> GetLastRefreshUtcAsync(CancellationToken ct)
    {
        return  _db.Breweries.MaxAsync(x => (DateTimeOffset?)x.LastRefreshedUtc, ct); 
    }
        
}