using AutoMapper.QueryableExtensions;
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
        var response = await _dbContext.Deals
            .AsNoTracking()
            .Where(d => d.Id == request.Id)
            .ProjectTo<DealResponse>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (response == null)
            return Result.Failure<DealResponse>(Error.NotFound("Deal", request.Id));

        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<DealResponse>(Error.Unauthorized());

        if (!user.IsCrmManager())
        {
            var userId = user.GetUserId();
            var hasAccess = await _dbContext.Deals.AnyAsync(d => d.Id == request.Id && d.OwnerUserId == userId, cancellationToken);
            if (!hasAccess)
                return Result.Failure<DealResponse>(Error.Forbidden("You do not have permission to view this deal."));
        }

        return Result.Success(response);
    }
}
