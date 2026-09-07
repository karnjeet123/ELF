
using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Application.Dtos;
using Elf.Brewery.Application.Options;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;
using Elf.Brewery.Domain.Enums;
namespace Elf.Brewery.Application.Sorting;

public sealed class NameSorter : IBrewerySorter
{
    public BrewerySortField Field => BrewerySortField.Name;

    public IEnumerable<DomainBrewery> Sort(IEnumerable<DomainBrewery> source, BreweryQuery query)
    {
        return query.Direction == SortDirection.Asc
            ? source.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
            : source.OrderByDescending(b => b.Name, StringComparer.OrdinalIgnoreCase);
    }
}