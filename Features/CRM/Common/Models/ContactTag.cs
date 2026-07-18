namespace CRM.Features.CRM.Common.Models;

public class ContactTag
{
    public Guid ContactId { get; set; }
    public Guid TagId { get; set; }

    public Contact Contact { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
