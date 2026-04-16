using Microsoft.Extensions.Caching.Memory;

namespace CardDuel.ServerApi.Services;

public interface ICacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan duration);
    void Remove(string key);
}

public sealed class InMemoryCacheService(IMemoryCache memoryCache) : ICacheService
{
    public T? Get<T>(string key)
    {
        memoryCache.TryGetValue(key, out T? value);
        return value;
    }

    public void Set<T>(string key, T value, TimeSpan duration)
    {
        memoryCache.Set(key, value, duration);
    }

    public void Remove(string key)
    {
        memoryCache.Remove(key);
    }
}
