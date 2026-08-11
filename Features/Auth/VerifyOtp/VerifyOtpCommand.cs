using CRM.Common.Wrappers;
using MediatR;

namespace CRM.Features.Auth.VerifyOtp;

public class VerifyOtpCommand : IRequest<Result<VerifyOtpResponse>>
{
    public VerifyOtpRequest Request { get; set; } = null!;
}
