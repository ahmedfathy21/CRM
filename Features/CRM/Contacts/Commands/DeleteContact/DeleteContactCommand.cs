using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace CRM.Features.CRM.Contacts.Commands.DeleteContact;

public record DeleteContactCommand(Guid Id) : IRequest<Result>;

public class DeleteContactCommandValidator : AbstractValidator<DeleteContactCommand>
{
    public DeleteContactCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Contact ID is required.");
    }
}

public class DeleteContactCommandHandler : IRequestHandler<DeleteContactCommand, Result>
{
    private readonly CrmDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteContactCommandHandler(
        CrmDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result> Handle(DeleteContactCommand request, CancellationToken cancellationToken)
    {
        var contact = await _dbContext.Contacts
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (contact == null)
            return Result.Failure(Error.NotFound("Contact", request.Id));

        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure(Error.Unauthorized());

        // Only Manager/Admin can delete contacts (as per design spec in crm_design.md)
        if (!user.IsCrmManager())
        {
            return Result.Failure(Error.Forbidden("Only managers can delete contacts."));
        }

        _dbContext.Contacts.Remove(contact);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
