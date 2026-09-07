using Elf.Brewery.Application.Dtos;
using Elf.Brewery.Domain.Enums;
using Elf.Brewery.Domain.Exceptions;
using Xunit;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;

namespace Elf.Brewery.Application.Tests.Sorting;

public class DistanceSorterTests
{
    private static DomainBrewery Make(string id, double lat, double lon) =>
        new() { Id = id, Name = id, Latitude = lat, Longitude = lon };

    [Fact]
    public void Field_IsDistance()
    {
        var sorter = new Elf.Brewery.Application.Sorting.DistanceSorter();

        Assert.Equal(BrewerySortField.Distance, sorter.Field);
    }

    [Fact]
    public void Sort_WithoutLatLong_ThrowsValidationException()
    {
        var sorter = new Elf.Brewery.Application.Sorting.DistanceSorter();
        var source = new[] { Make("a", 1, 1) };
        var query = new BreweryQuery { Latitude = null, Longitude = null };

        Assert.Throws<ValidationException>(() => sorter.Sort(source, query).ToList());
    }

    [Fact]
    public void Sort_Ascending_OrdersByClosestFirst()
    {
        var sorter = new Elf.Brewery.Application.Sorting.DistanceSorter();
        // origin near "close" brewery, "far" brewery is much further away.
        var close = Make("close", 40.71, -74.00);
        var far = Make("far", 34.05, -118.24);
        var query = new BreweryQuery
        {
            Latitude = 40.70,
            Longitude = -74.01,
            Direction = SortDirection.Asc
        };

        var result = sorter.Sort(new[] { far, close }, query).Select(b => b.Id).ToList();

        Assert.Equal(new[] { "close", "far" }, result);
    }
}
