using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

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

        services.AddDbContext<BreweryDbContext>(options =>
            options.UseSqlite(config.GetConnectionString("BreweryDb")));

        services.AddKeyedScoped<IBreweryRepository, SqliteBreweryRepository>(BreweryStorageKeys.Sqlite);
        services.AddKeyedSingleton<IBreweryRepository, InMemoryBreweryRepository>(BreweryStorageKeys.InMemory);

        services.AddScoped<ICacheService, MemoryCacheService>();


        return services;
    }
}