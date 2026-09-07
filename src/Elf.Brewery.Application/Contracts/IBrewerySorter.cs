using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;
using Elf.Brewery.Application.Dtos;
using Elf.Brewery.Domain.Enums;
namespace Elf.Brewery.Application.Contracts;

public interface IBrewerySorter
{
    BrewerySortField Field { get; }
    IEnumerable<DomainBrewery> Sort(IEnumerable<DomainBrewery> source, BreweryQuery query);
}
