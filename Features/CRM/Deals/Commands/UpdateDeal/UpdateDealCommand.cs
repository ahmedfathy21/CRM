using AutoMapper;
using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Deals.Commands.UpdateDeal;

public record UpdateDealCommand(
    Guid Id,
    string Title,
    decimal Value,
    string Currency,
    DateOnly? ExpectedCloseDate,
    Guid? ContactId,
    Guid? CompanyId
) : IRequest<Result<DealResponse>>;

public class UpdateDealCommandValidator : AbstractValidator<UpdateDealCommand>
{
    public UpdateDealCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Deal ID is required.");

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

public class UpdateDealCommandHandler : IRequestHandler<UpdateDealCommand, Result<DealResponse>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateDealCommandHandler(CrmDbContext dbContext, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<DealResponse>> Handle(UpdateDealCommand request, CancellationToken cancellationToken)
    {
        var deal = await _dbContext.Deals
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (deal == null)
            return Result.Failure<DealResponse>(Error.NotFound("Deal", request.Id));

        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<DealResponse>(Error.Unauthorized());

        if (!user.IsCrmManager() && deal.OwnerUserId != user.GetUserId())
            return Result.Failure<DealResponse>(Error.Forbidden("You do not have permission to update this deal."));

        deal.Title = request.Title;
        deal.Value = request.Value;
        deal.Currency = request.Currency.ToUpper();
        deal.ExpectedCloseDate = request.ExpectedCloseDate;
        deal.ContactId = request.ContactId;
        deal.CompanyId = request.CompanyId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (deal.ContactId.HasValue && (deal.Contact == null || deal.Contact.Id != deal.ContactId))
        {
            deal.Contact = await _dbContext.Contacts.FindAsync(new object[] { deal.ContactId }, cancellationToken);
        }

        if (deal.CompanyId.HasValue && (deal.Company == null || deal.Company.Id != deal.CompanyId))
        {
            deal.Company = await _dbContext.Companies.FindAsync(new object[] { deal.CompanyId }, cancellationToken);
        }

        var response = _mapper.Map<DealResponse>(deal);

        return Result.Success(response);
    }
}
