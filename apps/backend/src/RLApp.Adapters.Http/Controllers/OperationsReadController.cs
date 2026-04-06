using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Security;
using RLApp.Application.Queries;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[Route("api/v1/operations")]
public sealed class OperationsReadController : ControllerBase
{
    private readonly IMediator _mediator;

    public OperationsReadController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AuthorizationPolicies.SupportOrSupervisor)]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var activeCorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString()
            : correlationId;

        var result = await _mediator.Send(
            new GetOperationalDashboardSnapshotQuery(activeCorrelationId),
            cancellationToken);

        return Ok(result.Data);
    }
}
