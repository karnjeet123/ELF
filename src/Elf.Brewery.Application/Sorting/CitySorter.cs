using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Application.Dtos;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;
using Elf.Brewery.Domain.Enums;
namespace Elf.Brewery.Application.Sorting;

public sealed class CitySorter : IBrewerySorter
{
    public BrewerySortField Field => BrewerySortField.City;

    public IEnumerable<DomainBrewery> Sort(IEnumerable<DomainBrewery> source, BreweryQuery query)
    {
        return query.Direction == SortDirection.Asc
            ? source.OrderBy(b => b.City, StringComparer.OrdinalIgnoreCase)
            : source.OrderByDescending(b => b.City, StringComparer.OrdinalIgnoreCase);
    }
}