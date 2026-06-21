namespace CRM.Common.Caching;

public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan? CacheDuration { get; }
    string? CachePrefix { get; }
}
