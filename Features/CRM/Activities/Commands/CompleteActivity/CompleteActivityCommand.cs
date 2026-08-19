using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Activities.Commands.CompleteActivity;

public record CompleteActivityCommand(Guid Id) : IRequest<Result<Guid>>;

public class CompleteActivityCommandValidator : AbstractValidator<CompleteActivityCommand>
{
    public CompleteActivityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class CompleteActivityCommandHandler : IRequestHandler<CompleteActivityCommand, Result<Guid>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CompleteActivityCommandHandler(CrmDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<Guid>> Handle(CompleteActivityCommand request, CancellationToken cancellationToken)
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
            return Result.Failure<Guid>(Error.Forbidden("You do not have permission to complete this activity."));
        }

        if (activity.IsCompleted)
        {
            return Result.Failure<Guid>(Error.Validation("Activity is already completed."));
        }

        activity.IsCompleted = true;
        activity.CompletedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(activity.Id);
    }
}
