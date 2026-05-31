using Microsoft.AspNetCore.Identity;

namespace CRM.Common.Models;

public class AppUser : IdentityUser
{
    public UserRole Role { get; set; } = UserRole.SalesRep;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
