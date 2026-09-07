using System;
using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Elf.Brewery.Application.Sorting;
public sealed class BrewerySorterFactory : IBrewerySorterFactory
{
    private readonly IServiceProvider _serviceProvider;

    public BrewerySorterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IBrewerySorter Resolve(BrewerySortField field)
    {
        var sorter = _serviceProvider.GetKeyedService<IBrewerySorter>(field);
        if (sorter is null)
        {
            throw new NotSupportedException($"Sorting by {field} is not supported.");
        }
        return sorter;
    }
}