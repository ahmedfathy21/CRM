namespace CRM.Features.CRM.Common.DTOs;

public class DealResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public int Probability { get; set; }
    public DateOnly? ExpectedCloseDate { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ContactId { get; set; }
    public ContactSummaryDto? Contact { get; set; }
    public Guid? CompanyId { get; set; }
    public CompanySummaryDto? Company { get; set; }
    public string? OwnerUserId { get; set; }
    public List<ActivityResponse> Activities { get; set; } = [];
    public List<NoteResponse> Notes { get; set; } = [];
}
