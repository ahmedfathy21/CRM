using AutoMapper;
using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using CRM.Features.CRM.Common.Models;
using CRM.Features.CRM.Common.Models.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CRM.Features.CRM.Deals.Commands.CreateDeal;

public record CreateDealCommand(
    string Title,
    decimal Value,
    string Currency,
    DateOnly? ExpectedCloseDate,
    Guid? ContactId,
    Guid? CompanyId
) : IRequest<Result<DealResponse>>;

public class CreateDealCommandValidator : AbstractValidator<CreateDealCommand>
{
    public CreateDealCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Value)
            .GreaterThanOrEqualTo(0).WithMessage("Value cannot be negative.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be exactly 3 characters (e.g., USD).");

        RuleFor(x => x.ExpectedCloseDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Expected close date must be in the future.")
            .When(x => x.ExpectedCloseDate.HasValue);
    }
}

public class CreateDealCommandHandler : IRequestHandler<CreateDealCommand, Result<DealResponse>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateDealCommandHandler(CrmDbContext dbContext, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<DealResponse>> Handle(CreateDealCommand request, CancellationToken cancellationToken)
    {
        var deal = new Deal
        {
            Title = request.Title,
            Value = request.Value,
            Currency = request.Currency.ToUpper(),
            Stage = DealStage.Lead,
            Probability = 10,
            ExpectedCloseDate = request.ExpectedCloseDate,
            ContactId = request.ContactId,
            CompanyId = request.CompanyId,
            OwnerUserId = _httpContextAccessor.HttpContext?.User.GetUserId()
        };

        _dbContext.Deals.Add(deal);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (deal.ContactId.HasValue)
        {
            deal.Contact = await _dbContext.Contacts.FindAsync(new object[] { deal.ContactId }, cancellationToken);
        }

        if (deal.CompanyId.HasValue)
        {
            deal.Company = await _dbContext.Companies.FindAsync(new object[] { deal.CompanyId }, cancellationToken);
        }

        var response = _mapper.Map<DealResponse>(deal);

        return Result.Success(response);
    }
}
