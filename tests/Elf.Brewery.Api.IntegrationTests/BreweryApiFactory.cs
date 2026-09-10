using Elf.Brewery.Application.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;

namespace Elf.Brewery.Api.IntegrationTests;

/// <summary>
/// Returns a fixed brewery list instead of calling Open Brewery DB, so the suite is
/// deterministic and runs offline.
/// </summary>
public sealed class StubBreweryProvider : IBreweryProvider
{
    public static readonly IReadOnlyList<DomainBrewery> Breweries = new[]
    {
        new DomainBrewery
        {
            Id = "test-1",
            Name = "Cascade Brewing",
            City = "Portland",
            State = "Oregon",
            Country = "United States",
            Phone = "(503) 265-8603",
            BreweryType = "micro",
            Latitude = 45.5122,
            Longitude = -122.6587
        },
        new DomainBrewery
        {
            Id = "test-2",
            Name = "Alpine Beer Company",
            City = "Alpine",
            State = "California",
            Country = "United States",
            Phone = "(619) 445-2337",
            BreweryType = "micro",
            Latitude = 32.8351,
            Longitude = -116.7664
        },
        new DomainBrewery
        {
            Id = "test-3",
            Name = "Portland Brewing",
            City = "Seattle",
            State = "Washington",
            Country = "United States",
            Phone = null,
            BreweryType = "regional",
            Latitude = null,
            Longitude = null
        }
    };

    public Task<IReadOnlyList<DomainBrewery>> FetchAllAsync(CancellationToken ct) =>
        Task.FromResult(Breweries);
}

/// <summary>
/// Boots the real API pipeline (DI, auth, exception handling, versioning) in-process,
/// pointed at an isolated Sqlite file per test run so tests never touch the dev database.
/// </summary>
public sealed class BreweryApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"brewery-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BreweryDb"] = $"Data Source={_dbPath}",
                // Supplied explicitly so the suite never depends on a developer's local
                // user-secrets store and stays runnable on CI.
                ["Jwt:SigningKey"] = "integration-tests-only-signing-key-32-chars-minimum",
                ["StaticUser:Username"] = "admin@elfbeauty.com",
                // Hash of "admin@123" (the password the tests still submit via the login body),
                // generated once with Microsoft.AspNetCore.Identity.PasswordHasher<T>.
                ["StaticUser:PasswordHash"] = "AQAAAAIAAYagAAAAEMy/fa8x8SVCcfrP4bACBIHJtnRkcaOyi/6F7A3U+lqg67RrD9N1TiACc2S6lIO5jQ==",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the real HTTP-backed provider so refresh never hits the network.
            services.RemoveAll<IBreweryProvider>();
            services.AddSingleton<IBreweryProvider, StubBreweryProvider>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Sqlite pools connections by default, which can keep a file handle open briefly
        // after the host shuts down. Cleanup is best-effort: the OS temp folder is reclaimed
        // eventually, and a locked handle here should never fail the test run.
        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_dbPath);
            File.Delete(_dbPath + "-shm");
            File.Delete(_dbPath + "-wal");
        }
        catch (IOException)
        {
        }
    }
}

