
public interface IBreweryProvider
{
    Task<IReadOnlyList<Brewery>> FetchAllAsync(CancellationToken ct);
}