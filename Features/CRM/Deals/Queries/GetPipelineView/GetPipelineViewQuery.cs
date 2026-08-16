using AutoMapper;
using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using CRM.Features.CRM.Common.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Deals.Queries.GetPipelineView;

public record GetPipelineViewQuery(string? OwnerUserId = null) : IRequest<Result<PipelineViewResponse>>;

public class PipelineViewResponse
{
    public List<PipelineColumnResponse> Columns { get; set; } = [];
    public decimal TotalPipelineValue { get; set; }
}

public class PipelineColumnResponse
{
    public string Stage { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public int DealsCount { get; set; }
    public List<DealSummaryDto> Deals { get; set; } = [];
}

public class GetPipelineViewQueryHandler : IRequestHandler<GetPipelineViewQuery, Result<PipelineViewResponse>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetPipelineViewQueryHandler(CrmDbContext dbContext, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<PipelineViewResponse>> Handle(GetPipelineViewQuery request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<PipelineViewResponse>(Error.Unauthorized());

        var query = _dbContext.Deals.AsQueryable();

        if (!user.IsCrmManager())
        {
            query = query.Where(d => d.OwnerUserId == user.GetUserId());
        }
        else if (!string.IsNullOrEmpty(request.OwnerUserId))
        {
            query = query.Where(d => d.OwnerUserId == request.OwnerUserId);
        }

        // Only pull active deals for the pipeline view (or all, but group them)
        var deals = await query
            .Include(d => d.Company)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        var response = new PipelineViewResponse();

        foreach (DealStage stage in Enum.GetValues(typeof(DealStage)))
        {
            var stageDeals = deals.Where(d => d.Stage == stage).ToList();
            
            var column = new PipelineColumnResponse
            {
                Stage = stage.ToString(),
                TotalValue = stageDeals.Sum(d => d.Value),
                DealsCount = stageDeals.Count,
                Deals = _mapper.Map<List<DealSummaryDto>>(stageDeals)
            };

            response.Columns.Add(column);
        }

        response.TotalPipelineValue = response.Columns.Sum(c => c.TotalValue);

        return Result.Success(response);
    }
}
