using MediatR;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Requests;
using RLApp.Application.Commands;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[Route("api/cashier")]
public class CashierController : ControllerBase
{
    private readonly IMediator _mediator;

    public CashierController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /api/cashier/call-next
    /// </summary>
    [HttpPost("call-next")]
    public async Task<IActionResult> CallNext(
        [FromBody] CallNextAtCashierRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string correlationId,
        [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey)
    {
        var userId = User.Identity?.Name ?? "system";
        
        var command = new CallNextAtCashierCommand(request.QueueId, correlationId, userId);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { Error = result.Message, CorrelationId = correlationId });

        var dataProp = result.GetType().GetProperty("Data");
        var data = dataProp != null ? dataProp.GetValue(result) : result;
        return Ok(data);
    }

    /// <summary>
    /// POST /api/cashier/validate-payment
    /// </summary>
    [HttpPost("validate-payment")]
    public async Task<IActionResult> ValidatePayment(
        [FromBody] ValidatePaymentRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId)
    {
        var userId = User.Identity?.Name ?? "system";
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();

        // QueueId and PatientId normally resolved from TurnId lookup. Mapped loosely for adaptation phase:
        var command = new ValidatePaymentCommand("Q-DEFAULT", "PAT-DEFAULT", request.ValidatedAmount, activeCorrelationId, userId);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { Error = result.Message, CorrelationId = activeCorrelationId });

        var dataProp = result.GetType().GetProperty("Data");
        var data = dataProp != null ? dataProp.GetValue(result) : result;
        return Ok(data);
    }

    /// <summary>
    /// POST /api/cashier/mark-payment-pending
    /// </summary>
    [HttpPost("mark-payment-pending")]
    public async Task<IActionResult> MarkPaymentPending(
        [FromBody] MarkPaymentPendingRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId)
    {
        var userId = User.Identity?.Name ?? "system";
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();

        var command = new MarkPaymentPendingCommand("Q-DEFAULT", "PAT-DEFAULT", activeCorrelationId, userId);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { Error = result.Message, CorrelationId = activeCorrelationId });

        var dataProp = result.GetType().GetProperty("Data");
        var data = dataProp != null ? dataProp.GetValue(result) : result;
        return Ok(data);
    }

    /// <summary>
    /// POST /api/cashier/mark-absent
    /// </summary>
    [HttpPost("mark-absent")]
    public async Task<IActionResult> MarkAbsent(
        [FromBody] CashierMarkAbsentRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId)
    {
        var userId = User.Identity?.Name ?? "system";
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();

        var command = new MarkAbsenceCommand("Q-DEFAULT", "PAT-DEFAULT", "ROOM-CASHIER", activeCorrelationId, userId);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(new { Error = result.Message, CorrelationId = activeCorrelationId });

        var dataProp = result.GetType().GetProperty("Data");
        var data = dataProp != null ? dataProp.GetValue(result) : result;
        return Ok(data);
    }
}
