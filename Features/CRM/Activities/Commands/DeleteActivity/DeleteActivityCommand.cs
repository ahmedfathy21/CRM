using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Activities.Commands.DeleteActivity;

public record DeleteActivityCommand(Guid Id) : IRequest<Result<Guid>>;

public class DeleteActivityCommandValidator : AbstractValidator<DeleteActivityCommand>
{
    public DeleteActivityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeleteActivityCommandHandler : IRequestHandler<DeleteActivityCommand, Result<Guid>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteActivityCommandHandler(CrmDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<Guid>> Handle(DeleteActivityCommand request, CancellationToken cancellationToken)
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
            return Result.Failure<Guid>(Error.Forbidden("You do not have permission to delete this activity."));
        }

        // Soft Delete
        activity.IsDeleted = true;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(activity.Id);
    }
}
