using AutoMapper.QueryableExtensions;
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
        var response = await _dbContext.Companies
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .ProjectTo<CompanyResponse>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (response == null)
            return Result.Failure<CompanyResponse>(Error.NotFound("Company", request.Id));

        return Result.Success(response);
    }
}
