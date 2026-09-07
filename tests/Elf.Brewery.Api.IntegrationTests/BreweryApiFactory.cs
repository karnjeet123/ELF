using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Elf.Brewery.Api.IntegrationTests;

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
            });
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

