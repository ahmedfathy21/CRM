using AutoMapper;
using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Deals.Queries.GetDealById;

public record GetDealByIdQuery(Guid Id) : IRequest<Result<DealResponse>>;

public class GetDealByIdQueryHandler : IRequestHandler<GetDealByIdQuery, Result<DealResponse>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetDealByIdQueryHandler(CrmDbContext dbContext, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<DealResponse>> Handle(GetDealByIdQuery request, CancellationToken cancellationToken)
    {
        var deal = await _dbContext.Deals
            .Include(d => d.Contact)
            .Include(d => d.Company)
            .Include(d => d.Activities.OrderByDescending(a => a.CreatedAt))
            .Include(d => d.Notes.OrderByDescending(n => n.CreatedAt))
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (deal == null)
            return Result.Failure<DealResponse>(Error.NotFound("Deal", request.Id));

        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<DealResponse>(Error.Unauthorized());

        if (!user.IsCrmManager() && deal.OwnerUserId != user.GetUserId())
            return Result.Failure<DealResponse>(Error.Forbidden("You do not have permission to view this deal."));

        var response = _mapper.Map<DealResponse>(deal);

        return Result.Success(response);
    }
}
