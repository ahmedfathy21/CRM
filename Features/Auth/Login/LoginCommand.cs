using CRM.Common.Wrappers;
using MediatR;

namespace CRM.Features.Auth.Login;

public class LoginCommand : IRequest<Result<TokenResponse>>
{
    public LoginRequest Request { get; set; } = null!;
}
