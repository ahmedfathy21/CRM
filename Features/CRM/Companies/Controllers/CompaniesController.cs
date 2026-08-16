using CRM.Common.Extensions;
using CRM.Features.CRM.Common.DTOs;
using CRM.Features.CRM.Companies.Commands.CreateCompany;
using CRM.Features.CRM.Companies.Commands.DeleteCompany;
using CRM.Features.CRM.Companies.Commands.UpdateCompany;
using CRM.Features.CRM.Companies.Queries.GetCompaniesList;
using CRM.Features.CRM.Companies.Queries.GetCompanyById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Features.CRM.Companies.Controllers;

[Authorize]
[ApiController]
[Route("api/crm/companies")]
public class CompaniesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CompaniesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCompanyCommand command)
    {
        var result = await _mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetCompanyByIdQuery(id));
        return result.ToActionResult();
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] GetCompaniesListQuery query)
    {
        var result = await _mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CompanyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID in URL does not match ID in body.");

        var result = await _mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteCompanyCommand(id));
        return result.ToActionResult();
    }
}
