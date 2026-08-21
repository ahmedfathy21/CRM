using CRM.Features.CRM.Dashboard.Queries.GetCrmDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Features.CRM.Dashboard.Controllers;

[ApiController]
[Route("api/crm/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard([FromQuery] string? userId)
    {
        var result = await _mediator.Send(new GetCrmDashboardQuery(userId));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
