using CRM.Common.Services;
using CRM.Common.Wrappers;
using MediatR;

namespace CRM.Features.Auth.Logout;

public class LogoutHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async Task<Result> Handle(LogoutCommand command, CancellationToken ct)
    {
        await _refreshTokenService.RevokeAsync(command.Request.RefreshToken, ct);

        return Result.Success();
    }
}
