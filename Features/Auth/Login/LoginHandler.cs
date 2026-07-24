using CRM.Common.Models;
using CRM.Common.Services;
using CRM.Common.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CRM.Features.Auth.Login;

public class LoginHandler : IRequestHandler<LoginCommand, Result<TokenResponse>>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtService _jwtService;

    public LoginHandler(UserManager<AppUser> userManager, JwtService jwtService)
    {
        _userManager = userManager;
        _jwtService = jwtService;
    }

    public async Task<Result<TokenResponse>> Handle(LoginCommand command, CancellationToken ct)
    {
        var req = command.Request;

        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null)
            return Result.Failure<TokenResponse>(Error.Unauthorized("Invalid credentials."));

        var validPassword = await _userManager.CheckPasswordAsync(user, req.Password);
        if (!validPassword)
            return Result.Failure<TokenResponse>(Error.Unauthorized("Invalid credentials."));

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var token = _jwtService.GenerateToken(user);

        return Result.Success(new TokenResponse
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresAt = token.ExpiresAt,
        });
    }
}
