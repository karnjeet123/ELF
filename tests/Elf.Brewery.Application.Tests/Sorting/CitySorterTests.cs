using Elf.Brewery.Application.Dtos;
using Elf.Brewery.Domain.Enums;
using Xunit;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;

namespace Elf.Brewery.Application.Tests.Sorting;

public class CitySorterTests
{
    private static DomainBrewery Make(string city) => new() { Id = city, Name = city, City = city };

    [Fact]
    public void Field_IsCity()
    {
        var sorter = new Elf.Brewery.Application.Sorting.CitySorter();

        Assert.Equal(BrewerySortField.City, sorter.Field);
    }

    [Fact]
    public void Sort_Ascending_OrdersByCityCaseInsensitive()
    {
        var sorter = new Elf.Brewery.Application.Sorting.CitySorter();
        var source = new[] { Make("Wichita"), Make("austin"), Make("Denver") };
        var query = new BreweryQuery { Direction = SortDirection.Asc };

        var result = sorter.Sort(source, query).Select(b => b.City).ToList();

        Assert.Equal(new[] { "austin", "Denver", "Wichita" }, result);
    }

    [Fact]
    public void Sort_Descending_OrdersByCityCaseInsensitive()
    {
        var sorter = new Elf.Brewery.Application.Sorting.CitySorter();
        var source = new[] { Make("Wichita"), Make("austin"), Make("Denver") };
        var query = new BreweryQuery { Direction = SortDirection.Desc };

        var result = sorter.Sort(source, query).Select(b => b.City).ToList();

        Assert.Equal(new[] { "Wichita", "Denver", "austin" }, result);
    }
}
