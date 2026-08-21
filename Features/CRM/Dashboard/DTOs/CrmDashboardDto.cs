namespace CRM.Features.CRM.Dashboard.DTOs;

public class CrmDashboardDto
{
    // Financial Metrics
    public decimal TotalPipelineValue { get; set; }
    public decimal TotalRevenue { get; set; }

    // Advanced KPIs
    public Dictionary<string, decimal> RevenueByMonth { get; set; } = new();
    public double LeadConversionRate { get; set; }

    // Deal Metrics
    public int ActiveDealsCount { get; set; }
    public int WonDealsCount { get; set; }
    public double WinRatePercentage { get; set; }

    // Task Metrics
    public int OverdueTasksCount { get; set; }
    public int TasksDueTodayCount { get; set; }

    // Contact Metrics
    public int TotalActiveContacts { get; set; }

    // Breakdowns
    public Dictionary<string, int> DealsByStage { get; set; } = new();
    public Dictionary<string, int> ContactsByStatus { get; set; } = new();
}
