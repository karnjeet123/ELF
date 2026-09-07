using Microsoft.Extensions.DependencyInjection;
using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Application.Search;
using Elf.Brewery.Application.Services;
using Elf.Brewery.Application.Sorting;
using Elf.Brewery.Application.Options;

namespace Elf.Brewery.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IBrewerySearchService, BrewerySearchService>();
        services.AddSingleton<IBrewerySorter, NameSorter>();
        services.AddSingleton<IBrewerySorter, CitySorter>();
        services.AddSingleton<IBrewerySorter, DistanceSorter>();
        services.AddSingleton<IBrewerySorterFactory, BrewerySorterFactory>();

        // each key builds its own facade/service chain over the repository registered under the same key
        // v1 resolves the Sqlite key, v2 the in-memory key.
        services.AddKeyedScoped<IBreweryDataFacade, BreweryDataFacade>(BreweryStorageKeys.Sqlite);
        services.AddKeyedScoped<IBreweryService, BreweryService>(BreweryStorageKeys.Sqlite);

        services.AddKeyedScoped<IBreweryDataFacade, BreweryDataFacade>(BreweryStorageKeys.InMemory);
        services.AddKeyedScoped<IBreweryService, BreweryService>(BreweryStorageKeys.InMemory);

        return services;
    }
}