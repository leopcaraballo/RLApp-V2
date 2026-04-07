using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Requests;
using RLApp.Adapters.Http.Security;
using RLApp.Application.Commands;
using RLApp.Application.Services;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[Route("api/waiting-room")]
public class WaitingRoomController : RLAppControllerBase
{
    private readonly IMediator _mediator;

    public WaitingRoomController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/waiting-room/check-in
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.ReceptionOperations)]
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckInRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string correlationId,
        [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var patientName = string.IsNullOrWhiteSpace(request.PatientName)
            ? request.PatientId
            : request.PatientName;

        var command = new RegisterPatientArrivalCommand(
            request.QueueId,
            request.PatientId,
            patientName,
            request.AppointmentReference,
            int.TryParse(request.Priority, out var p) ? p : 0,
            request.Notes,
            correlationId,
            CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return FromCommandResult(result);
    }

    /// <summary>
    /// POST /api/waiting-room/call-patient
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.DoctorOperations)]
    [HttpPost("call-patient")]
    public async Task<IActionResult> CallPatient(
        [FromBody] CallPatientRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();
        var consultingRoomId = request.ResolveConsultingRoomId();

        if (string.IsNullOrWhiteSpace(consultingRoomId))
        {
            return BadRequest(new { Error = "consultingRoomId is required", CorrelationId = activeCorrelationId });
        }

        var patientId = !string.IsNullOrWhiteSpace(request.PatientId)
            ? request.PatientId
            : TurnReferenceParser.TryExtractPatientId(request.TurnId ?? string.Empty, request.QueueId, out var extractedPatientId)
                ? extractedPatientId
                : string.Empty;

        if (string.IsNullOrWhiteSpace(patientId))
        {
            return BadRequest(new { Error = "turnId must match queueId or patientId must be supplied", CorrelationId = activeCorrelationId });
        }

        var command = new CallPatientCommand(request.QueueId, patientId, consultingRoomId, activeCorrelationId, CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return FromCommandResult(result);
    }

    /// <summary>
    /// POST /api/waiting-room/claim-next
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.DoctorOperations)]
    [HttpPost("claim-next")]
    public async Task<IActionResult> ClaimNext(
        [FromBody] ClaimNextPatientRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();
        var consultingRoomId = request.ResolveConsultingRoomId();

        if (string.IsNullOrWhiteSpace(consultingRoomId))
        {
            return BadRequest(new { Error = "consultingRoomId is required", CorrelationId = activeCorrelationId });
        }

        var command = new ClaimNextPatientCommand(request.QueueId, consultingRoomId, activeCorrelationId, CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return FromCommandResult(result);
    }

    /// <summary>
    /// POST /api/waiting-room/complete-attention
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.DoctorOperations)]
    [HttpPost("complete-attention")]
    public async Task<IActionResult> CompleteAttention(
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
}
