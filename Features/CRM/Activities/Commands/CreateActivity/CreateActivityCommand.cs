using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.Models;
using CRM.Features.CRM.Common.Models.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Activities.Commands.CreateActivity;

public record CreateActivityCommand(
    ActivityType Type,
    string Subject,
    string? Description,
    DateTime? ScheduledAt,
    Guid? ContactId,
    Guid? DealId
) : IRequest<Result<Guid>>;

public class CreateActivityCommandValidator : AbstractValidator<CreateActivityCommand>
{
    public CreateActivityCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        
        RuleFor(x => x)
            .Must(x => x.ContactId.HasValue || x.DealId.HasValue)
            .WithMessage("An activity must be linked to either a Contact or a Deal.");
    }
}

public class CreateActivityCommandHandler : IRequestHandler<CreateActivityCommand, Result<Guid>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateActivityCommandHandler(CrmDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<Guid>> Handle(CreateActivityCommand request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<Guid>(Error.Unauthorized());

        var userId = user.GetUserId();

        if (request.ContactId.HasValue)
        {
            var contactExists = await _dbContext.Contacts.AnyAsync(c => c.Id == request.ContactId, cancellationToken);
            if (!contactExists) return Result.Failure<Guid>(Error.NotFound("Contact", request.ContactId.Value));
        }

        if (request.DealId.HasValue)
        {
            var dealExists = await _dbContext.Deals.AnyAsync(d => d.Id == request.DealId, cancellationToken);
            if (!dealExists) return Result.Failure<Guid>(Error.NotFound("Deal", request.DealId.Value));
        }

        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            Subject = request.Subject,
            Description = request.Description,
            ScheduledAt = request.ScheduledAt,
            ContactId = request.ContactId,
            DealId = request.DealId,
            CreatedByUserId = userId,
            IsCompleted = false,
            IsDeleted = false
        };

        _dbContext.Activities.Add(activity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(activity.Id);
    }
}
