using AutoMapper;
using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using CRM.Features.CRM.Common.Models.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace CRM.Features.CRM.Contacts.Queries.GetContactsList;

public record GetContactsListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    ContactStatus? Status = null,
    ContactSource? Source = null,
    Guid? CompanyId = null,
    string? AssignedToUserId = null
) : IRequest<Result<PagedResponse<ContactSummaryDto>>>;

public class GetContactsListQueryHandler : IRequestHandler<GetContactsListQuery, Result<PagedResponse<ContactSummaryDto>>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetContactsListQueryHandler(
        CrmDbContext dbContext,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<PagedResponse<ContactSummaryDto>>> Handle(GetContactsListQuery request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<PagedResponse<ContactSummaryDto>>(Error.Unauthorized());

        var query = _dbContext.Contacts.AsQueryable();

        // Data Scoping
        if (!user.IsCrmManager())
        {
            query = query.Where(c => c.AssignedToUserId == user.GetUserId());
        }
        else if (!string.IsNullOrEmpty(request.AssignedToUserId))
        {
            query = query.Where(c => c.AssignedToUserId == request.AssignedToUserId);
        }

        // Filters
        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(c => 
                c.FirstName.ToLower().Contains(search) || 
                c.LastName.ToLower().Contains(search) || 
                (c.Email != null && c.Email.ToLower().Contains(search)) || 
                (c.Phone != null && c.Phone.ToLower().Contains(search)));
        }

        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);

        if (request.Source.HasValue)
            query = query.Where(c => c.Source == request.Source.Value);

        if (request.CompanyId.HasValue)
            query = query.Where(c => c.CompanyId == request.CompanyId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var contacts = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var data = _mapper.Map<List<ContactSummaryDto>>(contacts);

        var response = new PagedResponse<ContactSummaryDto>
        {
            Data = data,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result.Success(response);
    }
}
