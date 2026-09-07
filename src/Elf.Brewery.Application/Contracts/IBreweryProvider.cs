using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;
namespace Elf.Brewery.Application.Contracts;
public interface IBreweryProvider
{
    Task<IReadOnlyList<DomainBrewery>> FetchAllAsync(CancellationToken ct);
}