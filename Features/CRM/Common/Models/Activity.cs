using CRM.Common.Models;
using CRM.Features.CRM.Common.Models.Enums;

namespace CRM.Features.CRM.Common.Models;

public class Activity : BaseEntity
{
    public ActivityType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? DealId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;

    public Contact? Contact { get; set; }
    public Deal? Deal { get; set; }
}
