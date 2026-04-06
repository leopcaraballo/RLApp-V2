using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Requests;
using RLApp.Adapters.Http.Security;
using RLApp.Application.Commands;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[Route("api/medical")]
public class MedicalController : RLAppControllerBase
{
    private readonly IMediator _mediator;

    public MedicalController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/medical/consulting-room/activate
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.SupervisorOnly)]
    [HttpPost("consulting-room/activate")]
    public async Task<IActionResult> ActivateRoom(
        [FromBody] ActivateConsultingRoomRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string correlationId,
        CancellationToken cancellationToken)
    {
        var command = new ActivateConsultingRoomCommand(request.RoomId, request.RoomName, correlationId, CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return FromCommandResult(result);
    }

    /// <summary>
    /// POST /api/medical/consulting-room/deactivate
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.SupervisorOnly)]
    [HttpPost("consulting-room/deactivate")]
    public async Task<IActionResult> DeactivateRoom(
        [FromBody] DeactivateConsultingRoomRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string correlationId,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateConsultingRoomCommand(request.RoomId, correlationId, CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return FromCommandResult(result);
    }

    /// <summary>
    /// POST /api/medical/finish-consultation
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.DoctorOperations)]
    [HttpPost("finish-consultation")]
    public async Task<IActionResult> FinishConsultation(
        [FromBody] FinishConsultationRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();

        var command = new FinishConsultationCommand(
            request.QueueId,
            request.PatientId,
            request.ConsultingRoomId,
            request.TurnId,
            request.Outcome,
            activeCorrelationId,
            CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return FromCommandResult(result);
    }

    /// <summary>
    /// POST /api/medical/mark-absent
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.DoctorOperations)]
    [HttpPost("mark-absent")]
    public async Task<IActionResult> MarkAbsent(
        [FromBody] MedicalMarkAbsentRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();

        var command = new MarkAbsenceAtConsultationCommand(
            request.QueueId,
            request.PatientId,
            request.ConsultingRoomId,
            request.TurnId,
            request.Reason,
            activeCorrelationId,
            CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return FromCommandResult(result);
    }
}
