using AutoMapper;
using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using CRM.Features.CRM.Common.Models;
using CRM.Features.CRM.Common.Models.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace CRM.Features.CRM.Contacts.Commands.UpdateContact;

public record UpdateContactCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? JobTitle,
    ContactStatus Status,
    ContactSource Source,
    Guid? CompanyId,
    List<Guid> TagIds
) : IRequest<Result<ContactResponse>>;

public class UpdateContactCommandValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Contact ID is required.");
        
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("Phone must not exceed 30 characters.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.JobTitle)
            .MaximumLength(150).WithMessage("Job title must not exceed 150 characters.")
            .When(x => !string.IsNullOrEmpty(x.JobTitle));

        RuleFor(x => x.Status).IsInEnum().WithMessage("Invalid status value.");
        RuleFor(x => x.Source).IsInEnum().WithMessage("Invalid source value.");
    }
}

public class UpdateContactCommandHandler : IRequestHandler<UpdateContactCommand, Result<ContactResponse>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateContactCommandHandler(
        CrmDbContext dbContext,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<ContactResponse>> Handle(UpdateContactCommand request, CancellationToken cancellationToken)
    {
        var contact = await _dbContext.Contacts
            .Include(c => c.ContactTags)
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (contact == null)
            return Result.Failure<ContactResponse>(Error.NotFound("Contact", request.Id));

        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<ContactResponse>(Error.Unauthorized());

        // Authorization Scoping logic
        if (!user.IsCrmManager() && contact.AssignedToUserId != user.GetUserId())
        {
            return Result.Failure<ContactResponse>(Error.Forbidden("You do not have permission to update this contact."));
        }

        if (!string.IsNullOrEmpty(request.Email) && request.Email != contact.Email)
        {
            var emailExists = await _dbContext.Contacts
                .AnyAsync(c => c.Email == request.Email, cancellationToken);
                
            if (emailExists)
                return Result.Failure<ContactResponse>(Error.Conflict("A contact with this email already exists."));
        }

        contact.FirstName = request.FirstName;
        contact.LastName = request.LastName;
        contact.Email = request.Email;
        contact.Phone = request.Phone;
        contact.JobTitle = request.JobTitle;
        contact.Status = request.Status;
        contact.Source = request.Source;
        contact.CompanyId = request.CompanyId;
        
        // Handle tags
        _dbContext.ContactTags.RemoveRange(contact.ContactTags);
        
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

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (contact.CompanyId.HasValue && (contact.Company == null || contact.Company.Id != contact.CompanyId))
        {
            contact.Company = await _dbContext.Companies.FindAsync(new object[] { contact.CompanyId }, cancellationToken);
        }
        
        var response = _mapper.Map<ContactResponse>(contact);

        return Result.Success(response);
    }
}
