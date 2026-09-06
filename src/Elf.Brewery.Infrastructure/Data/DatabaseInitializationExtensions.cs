using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeInfrastructureDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetService<BreweryDbContext>();
        if (database is null)
            return;

        await database.Database.EnsureCreatedAsync(cancellationToken);
    }
}