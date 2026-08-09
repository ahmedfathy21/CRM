using CRM.Common.Services;
using CRM.Common.Wrappers;

namespace CRM.Common.Extensions;

public static class TokenResultExtensions
{
    public static TokenResponse ToTokenResponse(this TokenResult token)
        => new()
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresAt = token.ExpiresAt,
        };
}
