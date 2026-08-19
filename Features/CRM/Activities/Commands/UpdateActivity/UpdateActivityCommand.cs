using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.Models.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Activities.Commands.UpdateActivity;

public record UpdateActivityCommand(
    Guid Id,
    ActivityType Type,
    string Subject,
    string? Description,
    DateTime? ScheduledAt,
    Guid? ContactId,
    Guid? DealId
) : IRequest<Result<Guid>>;

public class UpdateActivityCommandValidator : AbstractValidator<UpdateActivityCommand>
{
    public UpdateActivityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        
        RuleFor(x => x)
            .Must(x => x.ContactId.HasValue || x.DealId.HasValue)
            .WithMessage("An activity must be linked to either a Contact or a Deal.");
    }
}

public class UpdateActivityCommandHandler : IRequestHandler<UpdateActivityCommand, Result<Guid>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateActivityCommandHandler(CrmDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<Guid>> Handle(UpdateActivityCommand request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure<Guid>(Error.Unauthorized());

        var activity = await _dbContext.Activities
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (activity == null)
            return Result.Failure<Guid>(Error.NotFound("Activity", request.Id));

        if (!user.IsCrmManager() && activity.CreatedByUserId != user.GetUserId())
        {
            return Result.Failure<Guid>(Error.Forbidden("You do not have permission to update this activity."));
        }

        if (request.ContactId.HasValue && request.ContactId != activity.ContactId)
        {
            var contactExists = await _dbContext.Contacts.AnyAsync(c => c.Id == request.ContactId, cancellationToken);
            if (!contactExists) return Result.Failure<Guid>(Error.NotFound("Contact", request.ContactId.Value));
        }

        if (request.DealId.HasValue && request.DealId != activity.DealId)
        {
            var dealExists = await _dbContext.Deals.AnyAsync(d => d.Id == request.DealId, cancellationToken);
            if (!dealExists) return Result.Failure<Guid>(Error.NotFound("Deal", request.DealId.Value));
        }

        activity.Type = request.Type;
        activity.Subject = request.Subject;
        activity.Description = request.Description;
        activity.ScheduledAt = request.ScheduledAt;
        activity.ContactId = request.ContactId;
        activity.DealId = request.DealId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(activity.Id);
    }
}
