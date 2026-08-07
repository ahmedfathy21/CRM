using CRM.Common.Caching;

namespace CRM.Common.Services;




public interface IOtpService
{
    string Generate(string email);
    bool validate (string email, string code);
    void Invalidate(string code);
}
public class OtpService : IOtpService
{
    private readonly ICacheService _cacheService;
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(10);
    public OtpService(ICacheService  cacheService) => _cacheService = cacheService;
    public string Generate(string email)
    {
        var code = new Random().Next(100000, 999999).ToString();
        _cacheService.SetAsync($"otp:{email}", code, Expiry);
        return code;
    }

    public bool validate(string email, string code)
    {
        var storedCode = _cacheService.GetAsync<string>($"otp:{email}").Result;
       return storedCode == code;
        
    }
    

    public void Invalidate(string email)=> _cacheService.RemoveAsync($"otp:{email}");
  
}

