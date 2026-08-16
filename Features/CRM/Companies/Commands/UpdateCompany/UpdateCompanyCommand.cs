using AutoMapper;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Companies.Commands.UpdateCompany;

public record UpdateCompanyCommand(
    Guid Id,
    string Name,
    string? Industry,
    string? Website,
    string? Phone,
    string? Address,
    int EmployeeCount
) : IRequest<Result<CompanyResponse>>;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Company ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.");

        RuleFor(x => x.Industry)
            .MaximumLength(100).WithMessage("Industry must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Industry));

        RuleFor(x => x.Website)
            .MaximumLength(300).WithMessage("Website URL must not exceed 300 characters.")
            .When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("Phone must not exceed 30 characters.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.EmployeeCount)
            .GreaterThanOrEqualTo(0).WithMessage("Employee count cannot be negative.");
    }
}

public class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, Result<CompanyResponse>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdateCompanyCommandHandler(CrmDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Result<CompanyResponse>> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (company == null)
            return Result.Failure<CompanyResponse>(Error.NotFound("Company", request.Id));

        if (request.Name != company.Name)
        {
            var companyNameExists = await _dbContext.Companies
                .AnyAsync(c => c.Name.ToLower() == request.Name.ToLower(), cancellationToken);

            if (companyNameExists)
                return Result.Failure<CompanyResponse>(Error.Conflict("A company with this name already exists."));
        }

        company.Name = request.Name;
        company.Industry = request.Industry;
        company.Website = request.Website;
        company.Phone = request.Phone;
        company.Address = request.Address;
        company.EmployeeCount = request.EmployeeCount;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<CompanyResponse>(company);
        
        // Populate counts for response if needed, though usually Update returns just the updated fields or requeries.
        // For simplicity in Update, we can just return 0 or do a count query if absolutely necessary.
        // Let's keep it simple as this is an update response.
        response.ContactsCount = await _dbContext.Contacts.CountAsync(c => c.CompanyId == company.Id, cancellationToken);
        var openDeals = await _dbContext.Deals.Where(d => d.CompanyId == company.Id && d.ClosedAt == null).ToListAsync(cancellationToken);
        response.OpenDealsCount = openDeals.Count;
        response.OpenDealsValue = openDeals.Sum(d => d.Value);

        return Result.Success(response);
    }
}
