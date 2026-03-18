using MediatR;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Requests;
using RLApp.Application.Commands;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[Route("api/reception")]
public class ReceptionController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReceptionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/reception/register
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] ReceptionRegisterRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string correlationId,
        [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey)
    {
        var userId = User.Identity?.Name ?? "system";
        
        // This is an operational alias mapped to RegisterPatientArrivalCommand
        var queueId = $"Q-{DateTime.UtcNow:yyyy-MM-dd}-MAIN"; 
        var command = new RegisterPatientArrivalCommand(queueId, request.PatientId, "PatientName", correlationId, userId);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { Error = result.Message, CorrelationId = correlationId });

        var dataProp = result.GetType().GetProperty("Data");
        var data = dataProp != null ? dataProp.GetValue(result) : result;
        return Ok(data);
    }
}
