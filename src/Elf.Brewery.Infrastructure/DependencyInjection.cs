using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Infrastructure.Caching;
using Elf.Brewery.Infrastructure.Contracts;
using Elf.Brewery.Infrastructure.Data;
using Elf.Brewery.Infrastructure.External.Models;
using Elf.Brewery.Infrastructure.Mapper;
using Elf.Brewery.Infrastructure.Security;

namespace Elf.Brewery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        // registrations
        services.AddMemoryCache();

        services.AddSingleton<IBrewerySourceMapper, BreweryMapper>();
        services.AddSingleton<IBreweryDtoMapper, BreweryMapper>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddHttpClient<IBreweryProvider, OpenBreweryDbProvider>(client =>
        {
            client.BaseAddress = new Uri(config["OpenBreweryDb:BaseUrl"]!);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, n => TimeSpan.FromMilliseconds(300 * Math.Pow(2, n))))
          .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

        // Registered unconditionally because the Sqlite repository is always available as a keyed option.
        services.AddDbContext<BreweryDbContext>(options =>
            options.UseSqlite(config.GetConnectionString("BreweryDb")));
        // v1 resolves the Sqlite key, v2 the in-memory key.
        services.AddKeyedScoped<IBreweryRepository, SqliteBreweryRepository>(BreweryStorageKeys.Sqlite);
        services.AddKeyedSingleton<IBreweryRepository, InMemoryBreweryRepository>(BreweryStorageKeys.InMemory);

        services.AddScoped<ICacheService, MemoryCacheService>();


        return services;
    }
}