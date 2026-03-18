using MediatR;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Requests;
using RLApp.Application.Commands;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[Route("api/waiting-room")]
public class WaitingRoomController : ControllerBase
{
    private readonly IMediator _mediator;

    public WaitingRoomController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/waiting-room/check-in
    /// </summary>
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckInRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string correlationId,
        [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey)
    {
        var userId = User.Identity?.Name ?? "system";
        
        // Wait, RegisterPatientArrivalCommand in Application Layer expects QueueId and PatientName instead of AppointmentReference etc.
        // Assuming QueueId is generated or derived. To strictly map to command:
        var queueId = $"Q-{DateTime.UtcNow:yyyy-MM-dd}-MAIN"; // Simplified for this layer mapping
        var patientName = "Unknown"; // Needs to be fetched typically, but command dictates PatientName

        var command = new RegisterPatientArrivalCommand(queueId, request.PatientId, patientName, correlationId, userId);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { Error = result.Message, CorrelationId = correlationId });

        var dataProp = result.GetType().GetProperty("Data");
        var data = dataProp != null ? dataProp.GetValue(result) : result;
        return Ok(data);
    }

    /// <summary>
    /// POST /api/waiting-room/call-patient
    /// </summary>
    [HttpPost("call-patient")]
    public async Task<IActionResult> CallPatient(
        [FromQuery] string queueId, // Assuming passed in route or query if not in body
        [FromBody] CallPatientRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId)
    {
        var userId = User.Identity?.Name ?? "system";
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();

        var command = new CallPatientCommand(queueId ?? "DEFAULT", request.PatientId, request.RoomId, activeCorrelationId, userId);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { Error = result.Message, CorrelationId = activeCorrelationId });

        var dataProp = result.GetType().GetProperty("Data");
        var data = dataProp != null ? dataProp.GetValue(result) : result;
        return Ok(data);
    }
    
    /// <summary>
    /// POST /api/waiting-room/claim-next
    /// </summary>
    [HttpPost("claim-next")]
    public async Task<IActionResult> ClaimNext(
        [FromQuery] string queueId,
        [FromBody] ClaimNextPatientRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId)
    {
        var userId = User.Identity?.Name ?? "system";
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();

        var command = new ClaimNextPatientCommand(queueId ?? "DEFAULT", request.RoomId, activeCorrelationId, userId);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { Error = result.Message, CorrelationId = activeCorrelationId });

        var dataProp = result.GetType().GetProperty("Data");
        var data = dataProp != null ? dataProp.GetValue(result) : result;
        return Ok(data);
    }
}
