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
            if (result.IsNotFound)
                return NotFound(new { Error = result.Message, result.CorrelationId });
            return BadRequest(new { Error = result.Message, result.CorrelationId });
        }

        return Ok(result);
    }

    protected IActionResult FromCommandResult<T>(CommandResult<T> result)
    {
        if (!result.Success)
        {
            if (result.IsNotFound)
                return NotFound(new { Error = result.Message, result.CorrelationId });
            return BadRequest(new { Error = result.Message, result.CorrelationId });
        }

        return Ok(result.Data);
    }
}
