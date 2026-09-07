/// <summary>DI keys selecting the <see cref="IBreweryRepository"/> implementation.</summary>
namespace Elf.Brewery.Application.Contracts;

public static class BreweryStorageKeys
{
    public const string Sqlite = "sqlite";
    public const string InMemory = "inmemory";
}
