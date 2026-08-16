using AutoMapper;
using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using CRM.Features.CRM.Common.Models.Enums;
using CRM.Features.CRM.Common.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Deals.Commands.ChangeDealStage;

public record ChangeDealStageCommand(
    Guid Id,
    DealStage NewStage
) : IRequest<Result<DealResponse>>;

public class ChangeDealStageCommandValidator : AbstractValidator<ChangeDealStageCommand>
{
    public ChangeDealStageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Deal ID is required.");
        RuleFor(x => x.NewStage).IsInEnum().WithMessage("Invalid deal stage.");
    }
}

public class ChangeDealStageCommandHandler : IRequestHandler<ChangeDealStageCommand, Result<DealResponse>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChangeDealStageCommandHandler(CrmDbContext dbContext, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<DealResponse>> Handle(ChangeDealStageCommand request, CancellationToken cancellationToken)
    {
        var deal = await _dbContext.Deals
            .Include(d => d.Contact)
            .Include(d => d.Company)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (deal == null)
            return Result.Failure<DealResponse>(Error.NotFound("Deal", request.Id));

        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<DealResponse>(Error.Unauthorized());

        if (!user.IsCrmManager() && deal.OwnerUserId != user.GetUserId())
            return Result.Failure<DealResponse>(Error.Forbidden("You do not have permission to change this deal's stage."));

        var transitionResult = DealStageTransitionService.MoveToStage(deal.Stage, request.NewStage);
        if (!transitionResult.IsSuccess)
            return Result.Failure<DealResponse>(transitionResult.Error);

        deal.Stage = transitionResult.Value.NewStage;
        deal.Probability = transitionResult.Value.NewProbability;

        if (deal.Stage == DealStage.Won || deal.Stage == DealStage.Lost)
        {
            deal.ClosedAt = DateTime.UtcNow;
        }
        else
        {
            deal.ClosedAt = null; // In case they reopen a closed deal back to Lead
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<DealResponse>(deal);
        return Result.Success(response);
    }
}
