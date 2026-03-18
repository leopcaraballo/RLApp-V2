using MediatR;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Requests;
using RLApp.Application.Commands;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[Route("api/medical")]
public class MedicalController : ControllerBase
{
    private readonly IMediator _mediator;

    public MedicalController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/medical/consulting-room/activate
    /// </summary>
    [HttpPost("consulting-room/activate")]
    public async Task<IActionResult> ActivateRoom(
        [FromBody] ActivateConsultingRoomRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string correlationId)
    {
        var userId = User.Identity?.Name ?? "system";
        
        var command = new ActivateConsultingRoomCommand(request.RoomId, request.RoomName, correlationId, userId);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { Error = result.Message, CorrelationId = correlationId });

        var dataProp = result.GetType().GetProperty("Data");
        var data = dataProp != null ? dataProp.GetValue(result) : result;
        return Ok(data);
    }

    /// <summary>
    /// POST /api/medical/consulting-room/deactivate
    /// </summary>
    [HttpPost("consulting-room/deactivate")]
    public async Task<IActionResult> DeactivateRoom(
        [FromBody] DeactivateConsultingRoomRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string correlationId)
    {
        var userId = User.Identity?.Name ?? "system";
        
        var command = new DeactivateConsultingRoomCommand(request.RoomId, correlationId, userId);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { Error = result.Message, CorrelationId = correlationId });

        var dataProp = result.GetType().GetProperty("Data");
        var data = dataProp != null ? dataProp.GetValue(result) : result;
        return Ok(data);
    }

    /// <summary>
    /// POST /api/medical/finish-consultation
    /// </summary>
    [HttpPost("finish-consultation")]
    public async Task<IActionResult> FinishConsultation(
        [FromBody] FinishConsultationRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId)
    {
        var userId = User.Identity?.Name ?? "system";
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();

        var command = new FinishConsultationCommand("Q-DEFAULT", "PAT-DEFAULT", request.ConsultingRoomId, activeCorrelationId, userId);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { Error = result.Message, CorrelationId = activeCorrelationId });

        var dataProp = result.GetType().GetProperty("Data");
        var data = dataProp != null ? dataProp.GetValue(result) : result;
        return Ok(data);
    }

    /// <summary>
    /// POST /api/medical/mark-absent
    /// </summary>
    [HttpPost("mark-absent")]
    public async Task<IActionResult> MarkAbsent(
        [FromBody] MedicalMarkAbsentRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId)
    {
        var userId = User.Identity?.Name ?? "system";
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();

        var command = new MarkAbsenceAtConsultationCommand("Q-DEFAULT", "PAT-DEFAULT", request.ConsultingRoomId, activeCorrelationId, userId);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { Error = result.Message, CorrelationId = activeCorrelationId });

        var dataProp = result.GetType().GetProperty("Data");
        var data = dataProp != null ? dataProp.GetValue(result) : result;
        return Ok(data);
    }
}
