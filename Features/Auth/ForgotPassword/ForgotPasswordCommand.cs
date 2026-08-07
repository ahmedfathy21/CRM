using CRM.Common.Wrappers;
using MediatR;

namespace CRM.Features.Auth.ForgotPassword;

public class ForgotPasswordCommand : IRequest<Result<ForgotPasswordResponse>>
{
    public ForgotPasswordRequest Request { get; set; } = null!;
}
