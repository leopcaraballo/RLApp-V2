using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RLApp.Adapters.Http.Requests;
using RLApp.Adapters.Http.Security;
using RLApp.Application.Commands;

namespace RLApp.Adapters.Http.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.CashierOperations)]
[Route("api/cashier")]
public class CashierController : RLAppControllerBase
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
        [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new CallNextAtCashierCommand(request.QueueId, request.CashierStationId, correlationId, CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return FromCommandResult(result);
    }

    /// <summary>
    /// POST /api/cashier/validate-payment
    /// </summary>
    [HttpPost("validate-payment")]
    public async Task<IActionResult> ValidatePayment(
        [FromBody] ValidatePaymentRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();

        var command = new ValidatePaymentCommand(
            request.QueueId,
            request.PatientId,
            request.ValidatedAmount,
            request.TurnId,
            request.PaymentReference,
            activeCorrelationId,
            CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return FromCommandResult(result);
    }

    /// <summary>
    /// POST /api/cashier/mark-payment-pending
    /// </summary>
    [HttpPost("mark-payment-pending")]
    public async Task<IActionResult> MarkPaymentPending(
        [FromBody] MarkPaymentPendingRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();

        var command = new MarkPaymentPendingCommand(request.QueueId, request.PatientId, activeCorrelationId, CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return FromCommandResult(result);
    }

    /// <summary>
    /// POST /api/cashier/mark-absent
    /// </summary>
    [HttpPost("mark-absent")]
    public async Task<IActionResult> MarkAbsent(
        [FromBody] CashierMarkAbsentRequest request,
        [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
        CancellationToken cancellationToken)
    {
        var activeCorrelationId = correlationId ?? Guid.NewGuid().ToString();

        var command = new MarkAbsenceCommand(
            request.QueueId,
            request.PatientId,
            "ROOM-CASHIER", // Default RoomId for cashier
            request.TurnId,
            request.Reason,
            activeCorrelationId,
            CurrentUserId);
        var result = await _mediator.Send(command, cancellationToken);
        return FromCommandResult(result);
    }
}
