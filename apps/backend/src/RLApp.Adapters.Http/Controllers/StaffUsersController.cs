using MediatR;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Requests;
using RLApp.Adapters.Http.Responses;
using RLApp.Application.Commands;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[Route("api/staff/users")]
public class StaffUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public StaffUsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/staff/users/change-role
    /// Changes the role of a staff user.
    /// </summary>
    [HttpPost("change-role")]
    public async Task<IActionResult> ChangeRole(
        [FromBody] ChangeRoleRequest request, 
        [FromHeader(Name = "X-Correlation-Id")] string correlationId,
        [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey)
    {
        // Extract userId from User Claims when Authorization is present
        var userId = User.Identity?.Name ?? "system";
        
        var command = new ChangeStaffRoleCommand(request.StaffUserId, request.NewRole, correlationId, userId);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { Error = result.Message, CorrelationId = correlationId });

        var dataProp = result.GetType().GetProperty("Data");
        var data = dataProp != null ? dataProp.GetValue(result) : result;
        return Ok(data);
    }
}
