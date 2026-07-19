using CRM.Features.CRM.Common.Models.Enums;

namespace CRM.Features.CRM.Common.DTOs;

public class ContactResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? AssignedToUserId { get; set; }
    public List<TagDto> Tags { get; set; } = [];
    public List<DealSummaryDto> Deals { get; set; } = [];
    public List<ActivityResponse> RecentActivities { get; set; } = [];
}
