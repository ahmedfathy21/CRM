using CRM.Common.Caching;
using System.Security.Cryptography;
namespace CRM.Common.Services;

public interface IOtpService
{
    Task<string> Generate(string email);
    Task<bool> Validate (string email, string code, CancellationToken ct);
    Task Invalidate(string code, CancellationToken ct);
}
public class OtpService : IOtpService
{
    private readonly ICacheService _cacheService;
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(10);
    public OtpService(ICacheService  cacheService) => _cacheService = cacheService;
    public async Task<string> Generate(string email)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        await _cacheService.SetAsync($"otp:{email}", code, Expiry);
        return code;
    }

    public async Task<bool> Validate(string email, string code , CancellationToken ct)
    {
        var storedCode = await  _cacheService.GetAsync<string>($"otp:{email}", ct);
      return storedCode is not null && storedCode == code;
    }
    
    public Task Invalidate(string email, CancellationToken ct)=> _cacheService.RemoveAsync($"otp:{email}",ct);
  
}

