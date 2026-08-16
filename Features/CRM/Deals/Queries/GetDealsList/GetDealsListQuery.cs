using AutoMapper;
using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using CRM.Features.CRM.Common.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Deals.Queries.GetDealsList;

public record GetDealsListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    DealStage? Stage = null,
    Guid? CompanyId = null,
    string? OwnerUserId = null
) : IRequest<Result<PagedResponse<DealSummaryDto>>>;

public class GetDealsListQueryHandler : IRequestHandler<GetDealsListQuery, Result<PagedResponse<DealSummaryDto>>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetDealsListQueryHandler(CrmDbContext dbContext, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<PagedResponse<DealSummaryDto>>> Handle(GetDealsListQuery request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<PagedResponse<DealSummaryDto>>(Error.Unauthorized());

        var query = _dbContext.Deals.AsQueryable();

        if (!user.IsCrmManager())
        {
            query = query.Where(d => d.OwnerUserId == user.GetUserId());
        }
        else if (!string.IsNullOrEmpty(request.OwnerUserId))
        {
            query = query.Where(d => d.OwnerUserId == request.OwnerUserId);
        }

        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(d => d.Title.ToLower().Contains(search));
        }

        if (request.Stage.HasValue)
        {
            query = query.Where(d => d.Stage == request.Stage.Value);
        }

        if (request.CompanyId.HasValue)
        {
            query = query.Where(d => d.CompanyId == request.CompanyId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var deals = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(d => d.Company)
            .ToListAsync(cancellationToken);

        var data = _mapper.Map<List<DealSummaryDto>>(deals);

        var response = new PagedResponse<DealSummaryDto>
        {
            Data = data,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result.Success(response);
    }
}
