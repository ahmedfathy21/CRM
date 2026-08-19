using AutoMapper.QueryableExtensions;
using AutoMapper;
using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Activities.Queries.GetActivitiesList;

public record GetActivitiesListQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? ContactId = null,
    Guid? DealId = null,
    bool? IsCompleted = null
) : IRequest<Result<PagedResponse<ActivitySummaryDto>>>;

public class GetActivitiesListQueryHandler : IRequestHandler<GetActivitiesListQuery, Result<PagedResponse<ActivitySummaryDto>>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetActivitiesListQueryHandler(CrmDbContext dbContext, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<PagedResponse<ActivitySummaryDto>>> Handle(GetActivitiesListQuery request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<PagedResponse<ActivitySummaryDto>>(Error.Unauthorized());

        var query = _dbContext.Activities.AsQueryable();

        if (!user.IsCrmManager())
        {
            query = query.Where(a => a.CreatedByUserId == user.GetUserId());
        }

        if (request.ContactId.HasValue)
        {
            query = query.Where(a => a.ContactId == request.ContactId.Value);
        }

        if (request.DealId.HasValue)
        {
            query = query.Where(a => a.DealId == request.DealId.Value);
        }

        if (request.IsCompleted.HasValue)
        {
            query = query.Where(a => a.IsCompleted == request.IsCompleted.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .AsNoTracking()
            .OrderByDescending(a => a.ScheduledAt ?? a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<ActivitySummaryDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        var response = new PagedResponse<ActivitySummaryDto>
        {
            Data = data,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result.Success(response);
    }
}
