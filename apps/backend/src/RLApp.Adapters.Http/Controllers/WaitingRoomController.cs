using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Requests;
using RLApp.Adapters.Http.Security;
using RLApp.Application.Commands;

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

        var command = new CallPatientCommand(request.QueueId, request.PatientId, request.RoomId, activeCorrelationId, CurrentUserId);
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

        var command = new ClaimNextPatientCommand(request.QueueId, request.RoomId, activeCorrelationId, CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return FromCommandResult(result);
    }
}
