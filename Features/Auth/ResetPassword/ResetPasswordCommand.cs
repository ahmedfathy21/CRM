using CRM.Common.Wrappers;
using MediatR;

namespace CRM.Features.Auth.ResetPassword;

public class ResetPasswordCommand : IRequest<Result>
{
    public ResetPasswordRequest Request { get; set; } = null!;
}
