using AutoMapper;
using CRM.Common.Wrappers;
using CRM.Features.CRM.Common.Data;
using CRM.Features.CRM.Common.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Companies.Queries.GetCompanyById;

public record GetCompanyByIdQuery(Guid Id) : IRequest<Result<CompanyResponse>>;

public class GetCompanyByIdQueryHandler : IRequestHandler<GetCompanyByIdQuery, Result<CompanyResponse>>
{
    private readonly CrmDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetCompanyByIdQueryHandler(CrmDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Result<CompanyResponse>> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (company == null)
            return Result.Failure<CompanyResponse>(Error.NotFound("Company", request.Id));

        var response = _mapper.Map<CompanyResponse>(company);

        response.ContactsCount = await _dbContext.Contacts.CountAsync(c => c.CompanyId == company.Id, cancellationToken);
        var openDeals = await _dbContext.Deals.Where(d => d.CompanyId == company.Id && d.ClosedAt == null).ToListAsync(cancellationToken);
        response.OpenDealsCount = openDeals.Count;
        response.OpenDealsValue = openDeals.Sum(d => d.Value);

        return Result.Success(response);
    }
}
