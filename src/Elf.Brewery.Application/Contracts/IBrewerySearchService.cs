using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;
using Elf.Brewery.Application.Dtos;
namespace Elf.Brewery.Application.Contracts;

public interface IBrewerySearchService
{
    IEnumerable<DomainBrewery> Filter(IEnumerable<DomainBrewery> source, string? term, string? city = null);
    IReadOnlyList<AutocompleteItemDto> Suggest(IEnumerable<DomainBrewery> source, string term, int limit);
}
