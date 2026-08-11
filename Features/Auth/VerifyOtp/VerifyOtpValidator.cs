using FluentValidation;

namespace CRM.Features.Auth.VerifyOtp;

public class VerifyOtpValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Request.Code)
            .NotEmpty().WithMessage("Code is required.")
            .Matches(@"^\d{6}$").WithMessage("Code must be a 6-digit number.");
    }
}
