using Elf.Brewery.Application.Dtos;
using Elf.Brewery.Domain.Enums;
using Xunit;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;

namespace Elf.Brewery.Application.Tests.Sorting;

public class NameSorterTests
{
    private static DomainBrewery Make(string name) => new() { Id = name, Name = name };

    [Fact]
    public void Field_IsName()
    {
        var sorter = new Elf.Brewery.Application.Sorting.NameSorter();

        Assert.Equal(BrewerySortField.Name, sorter.Field);
    }

    [Fact]
    public void Sort_Ascending_OrdersByNameCaseInsensitive()
    {
        var sorter = new Elf.Brewery.Application.Sorting.NameSorter();
        var source = new[] { Make("Zeta"), Make("alpha"), Make("Beta") };
        var query = new BreweryQuery { Direction = SortDirection.Asc };

        var result = sorter.Sort(source, query).Select(b => b.Name).ToList();

        Assert.Equal(new[] { "alpha", "Beta", "Zeta" }, result);
    }

    [Fact]
    public void Sort_Descending_OrdersByNameCaseInsensitive()
    {
        var sorter = new Elf.Brewery.Application.Sorting.NameSorter();
        var source = new[] { Make("Zeta"), Make("alpha"), Make("Beta") };
        var query = new BreweryQuery { Direction = SortDirection.Desc };

        var result = sorter.Sort(source, query).Select(b => b.Name).ToList();

        Assert.Equal(new[] { "Zeta", "Beta", "alpha" }, result);
    }
}
