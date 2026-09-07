
namespace Elf.Brewery.Application.Contracts;

public interface ICacheService
{
    Task<(bool Found, T? Value)> TryGetAsync<T>(string key, CancellationToken ct);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct);
    void Remove(string key);
}