using Microsoft.EntityFrameworkCore;

public sealed class SqliteBreweryRepository(BreweryDbContext db) : IBreweryRepository
{
    public async Task<IReadOnlyList<Brewery>> GetAllAsync(CancellationToken ct)
        => await db.Breweries.AsNoTracking().ToListAsync(ct);

    public Task<Brewery?> GetByIdAsync(string id, CancellationToken ct)
        => db.Breweries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task UpsertRangeAsync(IEnumerable<Brewery> items, CancellationToken ct)
    {
        var incoming = items.ToList();
        var ids = incoming.Select(x => x.Id).ToHashSet();

        var existing = await db.Breweries
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        foreach (var item in incoming)
        {
            if (existing.TryGetValue(item.Id, out var current))
                db.Entry(current).CurrentValues.SetValues(item);
            else
                db.Breweries.Add(item);
        }

        await db.SaveChangesAsync(ct);
    }

    public Task<DateTimeOffset?> GetLastRefreshUtcAsync(CancellationToken ct)
        => db.Breweries.MaxAsync(x => (DateTimeOffset?)x.LastRefreshedUtc, ct);
}