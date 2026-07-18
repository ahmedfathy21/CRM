using CRM.Common.Models;
using CRM.Features.CRM.Common.Models.Enums;

namespace CRM.Features.CRM.Common.Models;

public class Contact : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public ContactStatus Status { get; set; } = ContactStatus.Lead;
    public ContactSource Source { get; set; } = ContactSource.Other;
    public Guid? CompanyId { get; set; }
    public string? AssignedToUserId { get; set; }

    public Company? Company { get; set; }
    public ICollection<Deal> Deals { get; set; } = new List<Deal>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<ContactTag> ContactTags { get; set; } = new List<ContactTag>();
}
