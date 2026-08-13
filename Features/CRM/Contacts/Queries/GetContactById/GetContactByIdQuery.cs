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
        var contact = await _dbContext.Contacts
            .Include(c => c.Company)
            .Include(c => c.ContactTags)
                .ThenInclude(ct => ct.Tag)
            .Include(c => c.Deals)
            .Include(c => c.Activities.OrderByDescending(a => a.CreatedAt).Take(5)) // recent 5 activities
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (contact == null)
            return Result.Failure<ContactResponse>(Error.NotFound("Contact", request.Id));

        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<ContactResponse>(Error.Unauthorized());

        // Authorization Scoping logic
        if (!user.IsCrmManager() && contact.AssignedToUserId != user.GetUserId())
        {
            return Result.Failure<ContactResponse>(Error.Forbidden("You do not have permission to view this contact."));
        }
        
        var response = _mapper.Map<ContactResponse>(contact);

        // Manually map tags if necessary (since MappingProfile ignored them or if we want them custom)
        // Wait, MappingProfile ignored Tags, Deals, RecentActivities, we must map them here.
        response.Tags = contact.ContactTags.Select(ct => _mapper.Map<TagDto>(ct.Tag)).ToList();
        response.Deals = contact.Deals.Select(d => _mapper.Map<DealSummaryDto>(d)).ToList();
        response.RecentActivities = contact.Activities.Select(a => _mapper.Map<ActivityResponse>(a)).ToList();

        return Result.Success(response);
    }
}
