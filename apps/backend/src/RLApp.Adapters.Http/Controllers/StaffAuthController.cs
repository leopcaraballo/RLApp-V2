using MediatR;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Requests;
using RLApp.Adapters.Http.Responses;
using RLApp.Application.Commands;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[Route("api/staff/auth")]
public class StaffAuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public StaffAuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/staff/auth/login
    /// Authenticates a staff user and returns an access token.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, [FromHeader(Name = "X-Correlation-Id")] string? correlationId)
    {
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();
        var command = new AuthenticateStaffCommand(request.Identifier, request.Password, activeCorrelationId);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { Error = result.Message, CorrelationId = activeCorrelationId });

        var dataProp = result.GetType().GetProperty("Data");
        var data = dataProp != null ? dataProp.GetValue(result) : result;
        return Ok(data);
    }
}
