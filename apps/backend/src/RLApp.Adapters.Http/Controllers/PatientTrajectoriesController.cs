using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Requests;
using RLApp.Adapters.Http.Security;
using RLApp.Application.Commands;
using RLApp.Application.Queries;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[Route("api/patient-trajectories")]
public sealed class PatientTrajectoriesController : RLAppControllerBase
{
    private readonly IMediator _mediator;

    public PatientTrajectoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AuthorizationPolicies.SupportOrSupervisor)]
    [HttpGet]
    public async Task<IActionResult> Discover(
        [FromQuery] string? patientId,
        [FromQuery] string? queueId,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var activeCorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString()
            : correlationId;

        var result = await _mediator.Send(
            new DiscoverPatientTrajectoriesQuery(patientId ?? string.Empty, queueId, activeCorrelationId),
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { Code = result.Message, result.CorrelationId });
        }

        return Ok(result.Data);
    }

    [Authorize(Policy = AuthorizationPolicies.SupportOrSupervisor)]
    [HttpGet("{trajectoryId}")]
    public async Task<IActionResult> GetById(
        [FromRoute] string trajectoryId,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var activeCorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString()
            : correlationId;

        var result = await _mediator.Send(
            new GetPatientTrajectoryQuery(trajectoryId, activeCorrelationId),
            cancellationToken);

        if (!result.Success)
        {
            return NotFound(new { Code = result.Message, result.CorrelationId });
        }

        return Ok(result.Data);
    }

    [Authorize(Policy = AuthorizationPolicies.SupportOnly)]
    [HttpPost("rebuild")]
    public async Task<IActionResult> Rebuild(
        [FromBody] RebuildPatientTrajectoriesRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string correlationId,
        [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new RebuildPatientTrajectoriesCommand(
            request.QueueId,
            request.PatientId,
            request.DryRun,
            idempotencyKey,
            correlationId,
            CurrentUserId);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.Success)
        {
            return Ok(result.Data);
        }

        if (string.Equals(result.Message, "TRAJECTORY_REBUILD_ALREADY_RUNNING", StringComparison.Ordinal))
        {
            return Conflict(new { Code = result.Message, result.CorrelationId });
        }

        return BadRequest(new { Code = result.Message, result.CorrelationId });
    }

    [Authorize(Policy = AuthorizationPolicies.SupportOrSupervisor)]
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(
        [FromQuery] string queueId,
        [FromQuery] string? stage,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var activeCorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString()
            : correlationId;

        var result = await _mediator.Send(
            new QueryActivePatientTrajectoriesQuery(queueId, stage, activeCorrelationId),
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { Code = result.Message, result.CorrelationId });
        }

        return Ok(result.Data);
    }

    [Authorize(Policy = AuthorizationPolicies.SupportOrSupervisor)]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string queueId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var activeCorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString()
            : correlationId;

        var result = await _mediator.Send(
            new QueryPatientTrajectoryHistoryQuery(queueId, from, to, activeCorrelationId),
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { Code = result.Message, result.CorrelationId });
        }

        return Ok(result.Data);
    }
}
