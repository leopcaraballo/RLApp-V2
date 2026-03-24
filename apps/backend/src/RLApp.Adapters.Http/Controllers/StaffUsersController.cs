using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Requests;
using RLApp.Adapters.Http.Security;
using RLApp.Application.Commands;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.SupervisorOnly)]
[Route("api/staff/users")]
public class StaffUsersController : RLAppControllerBase
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
        [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new ChangeStaffRoleCommand(request.StaffUserId, request.NewRole, request.Reason, correlationId, CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return FromCommandResult(result);
    }
}
