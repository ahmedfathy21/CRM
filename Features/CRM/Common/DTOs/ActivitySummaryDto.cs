namespace CRM.Features.CRM.Common.DTOs;

public class ActivitySummaryDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public bool IsCompleted { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? DealId { get; set; }
}
