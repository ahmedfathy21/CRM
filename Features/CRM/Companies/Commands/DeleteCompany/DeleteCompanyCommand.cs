using CRM.Common.Extensions;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Companies.Commands.DeleteCompany;

public record DeleteCompanyCommand(Guid Id) : IRequest<Result>;

public class DeleteCompanyCommandValidator : AbstractValidator<DeleteCompanyCommand>
{
    public DeleteCompanyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Company ID is required.");
    }
}

public class DeleteCompanyCommandHandler : IRequestHandler<DeleteCompanyCommand, Result>
{
    private readonly CrmDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteCompanyCommandHandler(CrmDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result> Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return Result.Failure(Error.Unauthorized());

        if (!user.IsCrmManager())
            return Result.Failure(Error.Forbidden("Only managers can delete companies."));

        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (company == null)
            return Result.Failure(Error.NotFound("Company", request.Id));

        _dbContext.Companies.Remove(company);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
