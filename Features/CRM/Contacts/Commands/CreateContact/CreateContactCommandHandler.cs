using AutoMapper;
using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using CRM.Features.CRM.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace CRM.Features.CRM.Contacts.Commands.CreateContact;

public class CreateContactCommandHandler : IRequestHandler<CreateContactCommand, Result<ContactResponse>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateContactCommandHandler(
        CrmDbContext dbContext,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<ContactResponse>> Handle(CreateContactCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Email))
        {
            var emailExists = await _dbContext.Contacts
                .AnyAsync(c => c.Email == request.Email, cancellationToken);
                
            if (emailExists)
                return Result.Failure<ContactResponse>(Error.Conflict("A contact with this email already exists."));
        }

        var contact = new Contact
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            JobTitle = request.JobTitle,
            Status = request.Status,
            Source = request.Source,
            CompanyId = request.CompanyId,
            AssignedToUserId = _httpContextAccessor.HttpContext?.User.GetUserId()
        };

        if (request.TagIds.Any())
        {
            var existingTags = await _dbContext.Tags
                .Where(t => request.TagIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            foreach (var tag in existingTags)
            {
                contact.ContactTags.Add(new ContactTag { TagId = tag.Id, ContactId = contact.Id });
            }
        }

        _dbContext.Contacts.Add(contact);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Load company explicitly if not loaded (or just map to null, but let's query it for response)
        if (contact.CompanyId.HasValue)
        {
            contact.Company = await _dbContext.Companies.FindAsync(new object[] { contact.CompanyId }, cancellationToken);
        }
        
        var response = _mapper.Map<ContactResponse>(contact);

        return Result.Success(response);
    }
}
