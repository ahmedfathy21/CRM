namespace CRM.Features.CRM.Common.DTOs;

public class NoteResponse
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ContactId { get; set; }
    public Guid? DealId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
}
