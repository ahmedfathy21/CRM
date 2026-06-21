using System.Text.Json;
using CRM.Common.Wrappers;
using MediatR;

namespace CRM.Common.Caching;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : class
{
    private readonly ICacheService _cache;

    public CachingBehavior(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not ICacheableQuery cacheable)
            return await next();

        var cached = await _cache.GetAsync<string>(cacheable.CacheKey, ct);
        if (cached is not null)
        {
            var deserialized = JsonSerializer.Deserialize<TResponse>(cached);
            if (deserialized is not null)
                return deserialized;
        }

        var response = await next();

        await _cache.SetAsync(cacheable.CacheKey, response, cacheable.CacheDuration, ct);

        return response;
    }
}
