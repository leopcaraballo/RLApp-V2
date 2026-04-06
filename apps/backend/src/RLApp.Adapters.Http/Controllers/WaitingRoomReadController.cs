using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Security;
using RLApp.Application.Queries;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[Route("api/v1/waiting-room")]
public sealed class WaitingRoomReadController : ControllerBase
{
    private readonly IMediator _mediator;

    public WaitingRoomReadController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AuthorizationPolicies.ReceptionOperations)]
    [HttpGet("{queueId}/monitor")]
    public async Task<IActionResult> GetMonitor(
        [FromRoute] string queueId,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var activeCorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString()
            : correlationId;

        var result = await _mediator.Send(
            new GetWaitingRoomMonitorSnapshotQuery(queueId, activeCorrelationId),
            cancellationToken);

        if (!result.Success)
        {
            return NotFound(new { Code = result.Message, result.CorrelationId });
        }

        return Ok(result.Data);
    }
}
