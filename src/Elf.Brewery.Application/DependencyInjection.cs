using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Application.Search;
using Elf.Brewery.Application.Services;
using Elf.Brewery.Application.Sorting;
using Elf.Brewery.Application.Options;
using Elf.Brewery.Domain.Enums;

namespace Elf.Brewery.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IBrewerySearchService, BrewerySearchService>();
        services.AddKeyedSingleton<IBrewerySorter, NameSorter>(BrewerySortField.Name);
        services.AddKeyedSingleton<IBrewerySorter, CitySorter>(BrewerySortField.City);
        services.AddKeyedSingleton<IBrewerySorter, DistanceSorter>(BrewerySortField.Distance);
        services.AddSingleton<IBrewerySorterFactory, BrewerySorterFactory>();

        // each key builds its own facade/service chain over the repository registered under the same key
        // v1 resolves the Sqlite key, v2 the in-memory key.
        foreach (var key in new[] { BreweryStorageKeys.Sqlite, BreweryStorageKeys.InMemory })
        {
            services.AddKeyedScoped<IBreweryDataFacade>(key, (sp, k) => new BreweryDataFacade(
                sp.GetRequiredKeyedService<IBreweryRepository>(k!),
                sp.GetRequiredService<IBreweryProvider>(),
                sp.GetRequiredService<ICacheService>(),
                sp.GetRequiredService<IOptions<CacheOptions>>(),
                sp.GetRequiredService<ILogger<BreweryDataFacade>>(),
                sp.GetRequiredService<TimeProvider>(),
                (string)k!));

            services.AddKeyedScoped<IBreweryService>(key, (sp, k) => new BreweryService(
                sp.GetRequiredKeyedService<IBreweryDataFacade>(k!),
                sp.GetRequiredService<IBrewerySearchService>(),
                sp.GetRequiredService<IBrewerySorterFactory>(),
                sp.GetRequiredService<IBreweryDtoMapper>()));
        }

        return services;
    }
}