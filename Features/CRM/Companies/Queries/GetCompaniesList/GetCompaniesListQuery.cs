using AutoMapper;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Companies.Queries.GetCompaniesList;

public record GetCompaniesListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Industry = null
) : IRequest<Result<PagedResponse<CompanySummaryDto>>>;

public class GetCompaniesListQueryHandler : IRequestHandler<GetCompaniesListQuery, Result<PagedResponse<CompanySummaryDto>>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetCompaniesListQueryHandler(CrmDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Result<PagedResponse<CompanySummaryDto>>> Handle(GetCompaniesListQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Companies.AsQueryable();

        // Filters
        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(search));
        }

        if (!string.IsNullOrEmpty(request.Industry))
        {
            query = query.Where(c => c.Industry == request.Industry);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var companies = await query
            .OrderBy(c => c.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var data = _mapper.Map<List<CompanySummaryDto>>(companies);

        var response = new PagedResponse<CompanySummaryDto>
        {
            Data = data,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result.Success(response);
    }
}
