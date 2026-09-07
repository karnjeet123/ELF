using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;
namespace Elf.Brewery.Application.Contracts;

public interface IBreweryDataFacade
{
    Task<IReadOnlyList<DomainBrewery>> GetAllAsync(CancellationToken ct);

    Task<int> RefreshFromExternalAsync(CancellationToken ct);
}
