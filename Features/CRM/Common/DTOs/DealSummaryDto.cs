namespace CRM.Features.CRM.Common.DTOs;

public class DealSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string OwnerUserName { get; set; } = string.Empty;
}
