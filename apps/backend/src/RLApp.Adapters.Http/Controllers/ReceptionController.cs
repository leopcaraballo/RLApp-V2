using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Requests;
using RLApp.Adapters.Http.Security;
using RLApp.Application.Commands;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ReceptionOperations)]
[Route("api/reception")]
public class ReceptionController : RLAppControllerBase
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
}
