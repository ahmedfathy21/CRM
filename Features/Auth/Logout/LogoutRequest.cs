namespace CRM.Features.Auth.Logout;

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
