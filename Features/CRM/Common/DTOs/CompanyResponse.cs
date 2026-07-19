namespace CRM.Features.CRM.Common.DTOs;

public class CompanyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public int EmployeeCount { get; set; }
    public int ContactsCount { get; set; }
    public int OpenDealsCount { get; set; }
    public decimal OpenDealsValue { get; set; }
}
