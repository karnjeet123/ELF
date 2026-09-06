public interface IBreweryRepository
{
    Task<IReadOnlyList<Brewery>> GetAllAsync(CancellationToken ct);
    Task<Brewery?> GetByIdAsync(string id, CancellationToken ct);
    Task UpsertRangeAsync(IEnumerable<Brewery> breweries, CancellationToken ct);
    Task<DateTimeOffset?> GetLastRefreshUtcAsync(CancellationToken ct);
}