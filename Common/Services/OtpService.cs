using System.Security.Cryptography;
using CRM.Common.Caching;

namespace CRM.Common.Services;

public interface IOtpService
{
    Task<string> GenerateAsync(string email, CancellationToken ct);
    Task<bool> ValidateAsync(string email, string code, CancellationToken ct);
    Task InvalidateAsync(string email, CancellationToken ct);
}

public class OtpService : IOtpService
{
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(10);
    private readonly ICacheService _cache;

    public OtpService(ICacheService cache) => _cache = cache;

    public async Task<string> GenerateAsync(string email, CancellationToken ct)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        await _cache.SetAsync($"otp:{email}", code, Expiry, ct);
        return code;
    }

    public async Task<bool> ValidateAsync(string email, string code, CancellationToken ct)
    {
        var stored = await _cache.GetAsync<string>($"otp:{email}", ct);
        return stored is not null && stored == code;
    }

    public Task InvalidateAsync(string email, CancellationToken ct)
        => _cache.RemoveAsync($"otp:{email}", ct);
}
