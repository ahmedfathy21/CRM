using CRM.Common.Extensions;
using CRM.Features.CRM.Common.DTOs;
using CRM.Features.CRM.Deals.Commands.ChangeDealStage;
using CRM.Features.CRM.Deals.Commands.CreateDeal;
using CRM.Features.CRM.Deals.Commands.DeleteDeal;
using CRM.Features.CRM.Deals.Commands.UpdateDeal;
using CRM.Features.CRM.Deals.Queries.GetDealById;
using CRM.Features.CRM.Deals.Queries.GetDealsList;
using CRM.Features.CRM.Deals.Queries.GetPipelineView;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Features.CRM.Deals.Controllers;

[Authorize]
[ApiController]
[Route("api/crm/deals")]
public class DealsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DealsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(DealResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDealCommand command)
    {
        var result = await _mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DealResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetDealByIdQuery(id));
        return result.ToActionResult();
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] GetDealsListQuery query)
    {
        var result = await _mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("pipeline")]
    [ProducesResponseType(typeof(PipelineViewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPipeline([FromQuery] GetPipelineViewQuery query)
    {
        var result = await _mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DealResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDealCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID in URL does not match ID in body.");

        var result = await _mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}/stage")]
    [ProducesResponseType(typeof(DealResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStage(Guid id, [FromBody] ChangeDealStageCommand command)
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
        var result = await _mediator.Send(new DeleteDealCommand(id));
        return result.ToActionResult();
    }
}
