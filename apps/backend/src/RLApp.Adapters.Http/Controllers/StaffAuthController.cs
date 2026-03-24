using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Requests;
using RLApp.Application.Commands;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/staff/auth")]
public class StaffAuthController : RLAppControllerBase
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
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();
        var command = new AuthenticateStaffCommand(request.Identifier, request.Password, activeCorrelationId);

        var result = await _mediator.Send(command, cancellationToken);
        return FromCommandResult(result);
    }
}
