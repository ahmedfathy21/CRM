using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Deals.Commands.DeleteDeal;

public record DeleteDealCommand(Guid Id) : IRequest<Result>;

public class DeleteDealCommandValidator : AbstractValidator<DeleteDealCommand>
{
    public DeleteDealCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Deal ID is required.");
    }
}

public class DeleteDealCommandHandler : IRequestHandler<DeleteDealCommand, Result>
{
    private readonly CrmDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteDealCommandHandler(CrmDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result> Handle(DeleteDealCommand request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure(Error.Unauthorized());

        if (!user.IsCrmManager())
            return Result.Failure(Error.Forbidden("Only managers can delete deals."));

        var deal = await _dbContext.Deals
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (deal == null)
            return Result.Failure(Error.NotFound("Deal", request.Id));

        _dbContext.Deals.Remove(deal);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
