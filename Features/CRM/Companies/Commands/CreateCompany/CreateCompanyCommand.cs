using AutoMapper;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using CRM.Features.CRM.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Companies.Commands.CreateCompany;

public record CreateCompanyCommand(
    string Name,
    string? Industry,
    string? Website,
    string? Phone,
    string? Address,
    int EmployeeCount
) : IRequest<Result<CompanyResponse>>;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
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

public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, Result<CompanyResponse>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;

    public CreateCompanyCommandHandler(CrmDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Result<CompanyResponse>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var companyNameExists = await _dbContext.Companies
            .AnyAsync(c => c.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (companyNameExists)
            return Result.Failure<CompanyResponse>(Error.Conflict("A company with this name already exists."));

        var company = new Company
        {
            Name = request.Name,
            Industry = request.Industry,
            Website = request.Website,
            Phone = request.Phone,
            Address = request.Address,
            EmployeeCount = request.EmployeeCount
        };

        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<CompanyResponse>(company);
        response.ContactsCount = 0;
        response.OpenDealsCount = 0;
        response.OpenDealsValue = 0;

        return Result.Success(response);
    }
}
