namespace CRM.Features.CRM.Common.DTOs;

public class ActivityResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? DealId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
}
