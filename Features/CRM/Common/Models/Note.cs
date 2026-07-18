using CRM.Common.Models;

namespace CRM.Features.CRM.Common.Models;

public class Note : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public Guid? ContactId { get; set; }
    public Guid? DealId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;

    public Contact? Contact { get; set; }
    public Deal? Deal { get; set; }
}
