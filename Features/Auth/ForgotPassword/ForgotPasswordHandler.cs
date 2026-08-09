using CRM.Common.Models;
using CRM.Common.Services;
using CRM.Common.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CRM.Features.Auth.ForgotPassword;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponse>>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IOtpService _otpService;

    public ForgotPasswordHandler(UserManager<AppUser> userManager, IOtpService otpService)
    {
        _userManager = userManager;
        _otpService = otpService;
    }

    public async Task<Result<ForgotPasswordResponse>> Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        var email = command.Request.Email;

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return Result.Success(new ForgotPasswordResponse());

        var code = await _otpService.GenerateAsync(email, ct);

        return Result.Success(new ForgotPasswordResponse { Otp = code });
    }
}
