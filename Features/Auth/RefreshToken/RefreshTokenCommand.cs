using CRM.Common.Wrappers;
using MediatR;

namespace CRM.Features.Auth.RefreshToken;

public class RefreshTokenCommand : IRequest<Result<TokenResponse>>
{
    public RefreshTokenRequest Request { get; set; } = null!;
}
