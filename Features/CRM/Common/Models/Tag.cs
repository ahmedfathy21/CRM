using CRM.Common.Models;

namespace CRM.Features.CRM.Common.Models;

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }

    public ICollection<ContactTag> ContactTags { get; set; } = new List<ContactTag>();
}
