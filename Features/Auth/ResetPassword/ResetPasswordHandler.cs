using CRM.Common.Models;
using CRM.Common.Services;
using CRM.Common.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CRM.Features.Auth.ResetPassword;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IOtpService _otpService;

    public ResetPasswordHandler(UserManager<AppUser> userManager, IOtpService otpService)
    {
        _userManager = userManager;
        _otpService = otpService;
    }

    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken ct)
    {
        var email = command.Request.Email;

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return Result.Success();

        var validOtp = await _otpService.ValidateAsync(email, command.Request.Code, ct);
        if (!validOtp)
            return Result.Failure(Error.Unauthorized("Invalid or expired OTP."));

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, command.Request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(Error.BadRequest(errors));
        }

        await _otpService.InvalidateAsync(email, ct);

        return Result.Success();
    }
}
