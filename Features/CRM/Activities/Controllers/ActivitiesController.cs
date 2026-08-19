using CRM.Features.CRM.Activities.Commands.CompleteActivity;
using CRM.Features.CRM.Activities.Commands.CreateActivity;
using CRM.Features.CRM.Activities.Commands.DeleteActivity;
using CRM.Features.CRM.Activities.Commands.UpdateActivity;
using CRM.Features.CRM.Activities.Queries.GetActivitiesList;
using CRM.Features.CRM.Activities.Queries.GetActivityById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Features.CRM.Activities.Controllers;

[ApiController]
[Route("api/crm/activities")]
[Authorize]
public class ActivitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ActivitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateActivity([FromBody] CreateActivityCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetActivityById), new { id = result.Value }, result.Value) 
            : BadRequest(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> GetActivities([FromQuery] GetActivitiesListQuery query)
    {
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetActivityById(Guid id)
    {
        var result = await _mediator.Send(new GetActivityByIdQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateActivity(Guid id, [FromBody] UpdateActivityCommand command)
    {
        if (id != command.Id) return BadRequest("Id mismatch.");
        
        var result = await _mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpPatch("{id}/complete")]
    public async Task<IActionResult> CompleteActivity(Guid id)
    {
        var result = await _mediator.Send(new CompleteActivityCommand(id));
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteActivity(Guid id)
    {
        var result = await _mediator.Send(new DeleteActivityCommand(id));
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
