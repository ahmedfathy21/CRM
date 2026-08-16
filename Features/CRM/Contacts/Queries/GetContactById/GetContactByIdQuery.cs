using AutoMapper.QueryableExtensions;
using AutoMapper;
using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace CRM.Features.CRM.Contacts.Queries.GetContactById;

public record GetContactByIdQuery(Guid Id) : IRequest<Result<ContactResponse>>;

public class GetContactByIdQueryHandler : IRequestHandler<GetContactByIdQuery, Result<ContactResponse>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetContactByIdQueryHandler(
        CrmDbContext dbContext,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<ContactResponse>> Handle(GetContactByIdQuery request, CancellationToken cancellationToken)
    {
        var response = await _dbContext.Contacts
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .ProjectTo<ContactResponse>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (response == null)
            return Result.Failure<ContactResponse>(Error.NotFound("Contact", request.Id));

        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<ContactResponse>(Error.Unauthorized());

        // Authorization Scoping logic
        // We have to query the AssignedToUserId separately if we don't include it in ContactResponse, 
        // but wait, is AssignedToUserId in ContactResponse? No.
        // Let's add an explicit DB check for scoping or add it to response. Let's do an explicit check in the DB query.

        if (!user.IsCrmManager())
        {
            var userId = user.GetUserId();
            var hasAccess = await _dbContext.Contacts.AnyAsync(c => c.Id == request.Id && c.AssignedToUserId == userId, cancellationToken);
            if (!hasAccess)
                return Result.Failure<ContactResponse>(Error.Forbidden("You do not have permission to view this contact."));
        }

        return Result.Success(response);
    }
}
