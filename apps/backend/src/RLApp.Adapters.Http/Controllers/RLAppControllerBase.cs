using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RLApp.Application.DTOs;

namespace RLApp.Adapters.Http.Controllers;

public abstract class RLAppControllerBase : ControllerBase
{
    protected string CurrentUserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("Authenticated user identifier is missing.");

    protected IActionResult FromCommandResult(CommandResult result)
    {
        if (!result.Success)
        {
            var errorPayload = new { Error = result.Message, Code = result.ErrorCode, result.CorrelationId };

            if (result.IsConflict)
                return Conflict(errorPayload);
            if (result.IsNotFound)
                return NotFound(errorPayload);
            return BadRequest(errorPayload);
        }

        return Ok(result);
    }

    protected IActionResult FromCommandResult<T>(CommandResult<T> result)
    {
        if (!result.Success)
        {
            var errorPayload = new { Error = result.Message, Code = result.ErrorCode, result.CorrelationId };

            if (result.IsConflict)
                return Conflict(errorPayload);
            if (result.IsNotFound)
                return NotFound(errorPayload);
            return BadRequest(errorPayload);
        }

        return Ok(result.Data);
    }
}
