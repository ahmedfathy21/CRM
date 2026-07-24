using CRM.Common.Models;
using CRM.Common.Services;
using CRM.Common.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CRM.Features.Auth.Register;

public class RegisterHandler : IRequestHandler<RegisterCommand, Result<TokenResponse>>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtService _jwtService;

    public RegisterHandler(UserManager<AppUser> userManager, JwtService jwtService)
    {
        _userManager = userManager;
        _jwtService = jwtService;
    }

    public async Task<Result<TokenResponse>> Handle(RegisterCommand command, CancellationToken ct)
    {
        var req = command.Request;

        var existingUser = await _userManager.FindByEmailAsync(req.Email);
        if (existingUser != null)
            return Result.Failure<TokenResponse>(Error.Conflict("Email already registered."));

        var role = Enum.TryParse<UserRole>(req.Role, out var parsed)
            ? parsed
            : UserRole.SalesRep;

        var user = new AppUser
        {
            UserName = req.Email,
            Email = req.Email,
            FirstName = req.FirstName,
            LastName = req.LastName,
            Role = role,
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<TokenResponse>(Error.BadRequest(errors));
        }

        var token = _jwtService.GenerateToken(user);

        return Result.Success(new TokenResponse
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresAt = token.ExpiresAt,
        });
    }
}
