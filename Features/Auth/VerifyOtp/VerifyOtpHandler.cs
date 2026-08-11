using CRM.Common.Services;
using CRM.Common.Wrappers;
using MediatR;

namespace CRM.Features.Auth.VerifyOtp;

public class VerifyOtpHandler : IRequestHandler<VerifyOtpCommand, Result<VerifyOtpResponse>>
{
    private readonly IOtpService _otpService;

    public VerifyOtpHandler(IOtpService otpService)
    {
        _otpService = otpService;
    }

    public async Task<Result<VerifyOtpResponse>> Handle(VerifyOtpCommand command, CancellationToken ct)
    {
        var valid = await _otpService.ValidateAsync(command.Request.Email, command.Request.Code, ct);
        if (!valid)
            return Result.Failure<VerifyOtpResponse>(Error.Unauthorized("Invalid or expired OTP."));

        return Result.Success(new VerifyOtpResponse { Valid = true });
    }
}
