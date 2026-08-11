using CRM.Common.Wrappers;
using MediatR;

namespace CRM.Features.Auth.Logout;

public class LogoutCommand : IRequest<Result>
{
    public LogoutRequest Request { get; set; } = null!;
}
