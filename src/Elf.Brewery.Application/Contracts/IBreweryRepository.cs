using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;
namespace Elf.Brewery.Application.Contracts;
public interface IBreweryRepository
{
    Task<IReadOnlyList<DomainBrewery>> GetAllAsync(CancellationToken ct);
    Task<DomainBrewery?> GetByIdAsync(string id, CancellationToken ct);
    Task UpsertRangeAsync(IEnumerable<DomainBrewery> breweries, CancellationToken ct);
    Task<DateTimeOffset?> GetLastRefreshUtcAsync(CancellationToken ct);
}