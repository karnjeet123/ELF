using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Application.Sorting;
using Elf.Brewery.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elf.Brewery.Application.Tests.Sorting;

public class BrewerySorterFactoryTests
{
    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IBrewerySorter, NameSorter>(BrewerySortField.Name);
        services.AddKeyedSingleton<IBrewerySorter, CitySorter>(BrewerySortField.City);
        services.AddKeyedSingleton<IBrewerySorter, DistanceSorter>(BrewerySortField.Distance);
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(BrewerySortField.Name, typeof(NameSorter))]
    [InlineData(BrewerySortField.City, typeof(CitySorter))]
    [InlineData(BrewerySortField.Distance, typeof(DistanceSorter))]
    public void Resolve_KnownField_ReturnsMatchingSorter(BrewerySortField field, Type expectedType)
    {
        var factory = new BrewerySorterFactory(BuildProvider());

        var sorter = factory.Resolve(field);

        Assert.IsType(expectedType, sorter);
    }

    [Fact]
    public void Resolve_UnknownField_ThrowsNotSupportedException()
    {
        // Build a provider with no sorters registered at all, to simulate an unmapped field.
        var emptyProvider = new ServiceCollection().BuildServiceProvider();
        var factory = new BrewerySorterFactory(emptyProvider);

        Assert.Throws<NotSupportedException>(() => factory.Resolve(BrewerySortField.Name));
    }
}
