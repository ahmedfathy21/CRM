using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.Models.Enums;
using CRM.Features.CRM.Dashboard.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CRM.Features.CRM.Dashboard.Queries.GetCrmDashboard;

public record GetCrmDashboardQuery(string? UserId = null) : IRequest<Result<CrmDashboardDto>>;

public class GetCrmDashboardQueryHandler : IRequestHandler<GetCrmDashboardQuery, Result<CrmDashboardDto>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _memoryCache;

    public GetCrmDashboardQueryHandler(CrmDbContext dbContext, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _memoryCache = memoryCache;
    }

    public async Task<Result<CrmDashboardDto>> Handle(GetCrmDashboardQuery request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<CrmDashboardDto>(Error.Unauthorized());

        // Data Scoping
        string targetUserId;
        if (!user.IsCrmManager())
        {
            targetUserId = user.GetUserId();
        }
        else
        {
            targetUserId = string.IsNullOrEmpty(request.UserId) ? string.Empty : request.UserId;
        }

        // Cache Key
        var cacheKey = $"crm_dashboard_{targetUserId}";
        if (_memoryCache.TryGetValue(cacheKey, out CrmDashboardDto? cachedResult) && cachedResult != null)
        {
            return Result.Success(cachedResult);
        }

        var dto = new CrmDashboardDto();

        // DEALS METRICS
        var dealsQuery = _dbContext.Deals.AsNoTracking();
        if (!string.IsNullOrEmpty(targetUserId))
            dealsQuery = dealsQuery.Where(d => d.OwnerUserId == targetUserId);

        var allDeals = await dealsQuery
            .Select(d => new { d.Stage, d.Value, d.ClosedAt })
            .ToListAsync(cancellationToken);

        var wonDeals = allDeals.Where(d => d.Stage == DealStage.Won).ToList();
        var lostDeals = allDeals.Where(d => d.Stage == DealStage.Lost).ToList();
        var activeDeals = allDeals.Where(d => d.Stage != DealStage.Won && d.Stage != DealStage.Lost).ToList();

        dto.ActiveDealsCount = activeDeals.Count;
        dto.WonDealsCount = wonDeals.Count;
        dto.TotalPipelineValue = activeDeals.Sum(d => d.Value);
        dto.TotalRevenue = wonDeals.Sum(d => d.Value);

        var totalFinishedDeals = wonDeals.Count + lostDeals.Count;
        dto.WinRatePercentage = totalFinishedDeals > 0 
            ? Math.Round((double)wonDeals.Count / totalFinishedDeals * 100, 2) 
            : 0;

        dto.DealsByStage = allDeals
            .GroupBy(d => d.Stage.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // REVENUE BY MONTH (Current Year)
        var currentYear = DateTime.UtcNow.Year;
        dto.RevenueByMonth = wonDeals
            .Where(d => d.ClosedAt.HasValue && d.ClosedAt.Value.Year == currentYear)
            .GroupBy(d => d.ClosedAt!.Value.ToString("MMM"))
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Value));

        // LEAD CONVERSION RATE
        var totalLeads = allDeals.Count; 
        dto.LeadConversionRate = totalLeads > 0 
            ? Math.Round((double)wonDeals.Count / totalLeads * 100, 2) 
            : 0;

        // CONTACTS METRICS
        var contactsQuery = _dbContext.Contacts.AsNoTracking();
        if (!string.IsNullOrEmpty(targetUserId))
            contactsQuery = contactsQuery.Where(c => c.AssignedToUserId == targetUserId);

        var contactsStatuses = await contactsQuery
            .Select(c => c.Status)
            .ToListAsync(cancellationToken);

        dto.TotalActiveContacts = contactsStatuses.Count(s => s != ContactStatus.Inactive && s != ContactStatus.Churned);
        dto.ContactsByStatus = contactsStatuses
            .GroupBy(s => s.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // TASKS METRICS
        var tasksQuery = _dbContext.Activities.AsNoTracking().Where(a => !a.IsCompleted);
        if (!string.IsNullOrEmpty(targetUserId))
            tasksQuery = tasksQuery.Where(a => a.CreatedByUserId == targetUserId);

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var activeTasks = await tasksQuery
            .Select(a => a.ScheduledAt)
            .ToListAsync(cancellationToken);

        dto.OverdueTasksCount = activeTasks.Count(d => d.HasValue && d.Value < today);
        dto.TasksDueTodayCount = activeTasks.Count(d => d.HasValue && d.Value >= today && d.Value < tomorrow);

        // Cache the result for 5 minutes
        _memoryCache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));

        return Result.Success(dto);
    }
}
