using CRM.Common.Caching;

namespace CRM.Common.Services;

public interface IRefreshTokenService
{
    Task StoreAsync(RefreshToken token, CancellationToken ct);
    Task<RefreshToken?> GetAsync(string token, CancellationToken ct);
    Task MarkUsedAsync(string token, CancellationToken ct);
    Task RevokeAsync(string token, CancellationToken ct);
}

public class InMemoryRefreshTokenService : IRefreshTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(7);
    private readonly ICacheService _cache;

    public InMemoryRefreshTokenService(ICacheService cache) => _cache = cache;

    public Task StoreAsync(RefreshToken token, CancellationToken ct)
        => _cache.SetAsync($"refresh:{token.Token}", token, TokenLifetime, ct);

    public Task<RefreshToken?> GetAsync(string token, CancellationToken ct)
        => _cache.GetAsync<RefreshToken>($"refresh:{token}", ct);

    public async Task MarkUsedAsync(string token, CancellationToken ct)
    {
        var stored = await GetAsync(token, ct);
        if (stored is null)
            return;

        stored.IsUsed = true;
        await _cache.SetAsync($"refresh:{token}", stored, TokenLifetime, ct);
    }

    public async Task RevokeAsync(string token, CancellationToken ct)
    {
        var stored = await GetAsync(token, ct);
        if (stored is null)
            return;

        stored.IsRevoked = true;
        await _cache.SetAsync($"refresh:{token}", stored, TokenLifetime, ct);
    }
}
