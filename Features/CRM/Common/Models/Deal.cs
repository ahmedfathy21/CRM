using CRM.Common.Models;
using CRM.Features.CRM.Common.Models.Enums;

namespace CRM.Features.CRM.Common.Models;

public class Deal : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Currency { get; set; } = "USD";
    public DealStage Stage { get; set; } = DealStage.Lead;
    public int Probability { get; set; } = 10;
    public DateOnly? ExpectedCloseDate { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? OwnerUserId { get; set; }

    public Contact? Contact { get; set; }
    public Company? Company { get; set; }
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();
}
