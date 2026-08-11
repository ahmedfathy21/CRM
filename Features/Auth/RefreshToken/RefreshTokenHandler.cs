using CRM.Common.Extensions;
using CRM.Common.Models;
using CRM.Common.Services;
using CRM.Common.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CRM.Features.Auth.RefreshToken;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<TokenResponse>>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtService _jwtService;

    public RefreshTokenHandler(
        IRefreshTokenService refreshTokenService,
        UserManager<AppUser> userManager,
        JwtService jwtService)
    {
        _refreshTokenService = refreshTokenService;
        _userManager = userManager;
        _jwtService = jwtService;
    }

    public async Task<Result<TokenResponse>> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        var token = command.Request.RefreshToken;

        var stored = await _refreshTokenService.GetAsync(token, ct);
        if (stored is null)
            return Result.Failure<TokenResponse>(Error.Unauthorized("Invalid refresh token."));

        if (stored.IsUsed)
            return Result.Failure<TokenResponse>(Error.Unauthorized("Refresh token has already been used."));

        if (stored.IsRevoked)
            return Result.Failure<TokenResponse>(Error.Unauthorized("Refresh token has been revoked."));

        if (stored.ExpiresAt < DateTime.UtcNow)
            return Result.Failure<TokenResponse>(Error.Unauthorized("Refresh token has expired."));

        var user = await _userManager.FindByIdAsync(stored.UserId);
        if (user is null)
            return Result.Failure<TokenResponse>(Error.Unauthorized("Invalid refresh token."));

        await _refreshTokenService.MarkUsedAsync(token, ct);

        var newTokens = await _jwtService.GenerateTokenAsync(user, ct);
        return Result.Success(newTokens.ToTokenResponse());
    }
}
