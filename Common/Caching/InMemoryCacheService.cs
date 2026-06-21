using Microsoft.Extensions.Caching.Memory;

namespace CRM.Common.Caching;

public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public InMemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        if (_cache.TryGetValue(key, out T? cached))
            return cached!;

        var value = await factory();
        SetCacheEntry(key, value, expiry);
        return value;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        SetCacheEntry(key, value, expiry);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        if (_cache is MemoryCache memoryCache)
        {
            var keys = memoryCache.Keys
                .OfType<string>()
                .Where(k => k.StartsWith(prefix))
                .ToList();

            foreach (var key in keys)
                memoryCache.Remove(key);
        }

        return Task.CompletedTask;
    }

    private void SetCacheEntry<T>(string key, T value, TimeSpan? expiry)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
            options.SetAbsoluteExpiration(expiry.Value);

        _cache.Set(key, value, options);
    }
}
