using CRM.Common.Wrappers;
using MediatR;

namespace CRM.Features.Auth.Register;

public class RegisterCommand : IRequest<Result<TokenResponse>>
{
    public RegisterRequest Request { get; set; } = null!;
}
