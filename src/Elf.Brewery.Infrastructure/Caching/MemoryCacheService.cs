using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<(bool Found, T? Value)> TryGetAsync<T>(string key, CancellationToken ct)
    {
        var found = _memoryCache.TryGetValue(key, out T? value) && value is not null;
        _logger.LogInformation(found ? "Cache hit for key: {Key}" : "Cache miss for key: {Key}", key);
        return Task.FromResult((found, value));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        _memoryCache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            Priority = CacheItemPriority.High
        });
        _logger.LogInformation("Cache set for key: {Key}", key);
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
    }
}