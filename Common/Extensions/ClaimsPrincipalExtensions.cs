using System.Security.Claims;
using CRM.Common.Constants;

namespace CRM.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return id ?? throw new UnauthorizedAccessException("User identifier not found in token.");
    }

    public static string GetUserRole(this ClaimsPrincipal user)
    {
        var role = user.FindFirstValue(ClaimTypes.Role);
        return role ?? throw new UnauthorizedAccessException("User role not found in token.");
    }

    public static bool IsCrmManager(this ClaimsPrincipal user)
    {
        var role = user.GetUserRole();
        return role is RoleConstants.Admin or RoleConstants.SalesManager;
    }

    public static bool IsSalesRep(this ClaimsPrincipal user)
    {
        var role = user.GetUserRole();
        return role == RoleConstants.SalesRep;
    }
}
