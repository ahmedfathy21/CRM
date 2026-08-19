using AutoMapper.QueryableExtensions;
using AutoMapper;
using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Activities.Queries.GetActivityById;

public record GetActivityByIdQuery(Guid Id) : IRequest<Result<ActivityResponse>>;

public class GetActivityByIdQueryHandler : IRequestHandler<GetActivityByIdQuery, Result<ActivityResponse>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetActivityByIdQueryHandler(CrmDbContext dbContext, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<ActivityResponse>> Handle(GetActivityByIdQuery request, CancellationToken cancellationToken)
    {
        var response = await _dbContext.Activities
            .AsNoTracking()
            .Where(a => a.Id == request.Id)
            .ProjectTo<ActivityResponse>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (response == null)
            return Result.Failure<ActivityResponse>(Error.NotFound("Activity", request.Id));

        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<ActivityResponse>(Error.Unauthorized());

        if (!user.IsCrmManager())
        {
            var userId = user.GetUserId();
            var hasAccess = await _dbContext.Activities
                .AnyAsync(a => a.Id == request.Id && a.CreatedByUserId == userId, cancellationToken);
                
            if (!hasAccess)
                return Result.Failure<ActivityResponse>(Error.Forbidden("You do not have permission to view this activity."));
        }

        return Result.Success(response);
    }
}
