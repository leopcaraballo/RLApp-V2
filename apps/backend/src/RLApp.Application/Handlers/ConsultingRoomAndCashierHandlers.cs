namespace RLApp.Application.Handlers;

using Commands;
using DTOs;
using RLApp.Domain.Common;
using RLApp.Ports.Inbound;

/// <summary>
/// Handler for UC-003: Activate Consulting Room
/// Activates a consulting room for daily operations.
/// Reference: S-002 Consulting Room Lifecycle
/// </summary>
public class ActivateConsultingRoomHandler
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;

    public ActivateConsultingRoomHandler(IWaitingQueueRepository queueRepository, IEventPublisher eventPublisher)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<CommandResult> Handle(ActivateConsultingRoomCommand command)
    {
        try
        {
            // TODO: In a real scenario, there would be a ConsultingRoom aggregate
            // For now, this demonstrates the pattern
            
            return CommandResult.Ok(command.CorrelationId, $"Consulting room {command.RoomName} activated");
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Activation failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-007: Call Next At Cashier
/// Calls next patient at cashier for payment validation.
/// Reference: S-004 Cashier Flow
/// </summary>
public class CallNextAtCashierHandler
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;

    public CallNextAtCashierHandler(IWaitingQueueRepository queueRepository, IEventPublisher eventPublisher)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<CommandResult<PatientCallResultDto>> Handle(CallNextAtCashierCommand command)
    {
        try
        {
            var queue = await _queueRepository.GetByIdAsync(command.QueueId);

            if (queue == null)
                return CommandResult<PatientCallResultDto>.Failure("Queue not found", command.CorrelationId);

            var patientId = queue.GetNextPatient();

            queue.CallPatient(patientId, null, command.CorrelationId);

            await _queueRepository.UpdateAsync(queue);

            var events = queue.GetUnraisedEvents();
            await _eventPublisher.PublishBatchAsync(events);
            queue.ClearUnraisedEvents();

            var result = new PatientCallResultDto
            {
                PatientId = patientId,
                CalledAt = DateTime.UtcNow,
                QueuePosition = 1
            };

            return CommandResult<PatientCallResultDto>.Ok(result, command.CorrelationId, "Patient called successfully");
        }
        catch (DomainException ex)
        {
            return CommandResult<PatientCallResultDto>.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            return CommandResult<PatientCallResultDto>.Failure($"Call failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// DTO for patient call result.
/// </summary>
public class PatientCallResultDto
{
    public string PatientId { get; set; }
    public DateTime CalledAt { get; set; }
    public int QueuePosition { get; set; }
}

/// <summary>
/// Handler for UC-008: Validate Payment
/// Validates patient payment.
/// Reference: S-004 Cashier Flow
/// </summary>
public class ValidatePaymentHandler
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;

    public ValidatePaymentHandler(IWaitingQueueRepository queueRepository, IEventPublisher eventPublisher)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<CommandResult> Handle(ValidatePaymentCommand command)
    {
        try
        {
            var queue = await _queueRepository.GetByIdAsync(command.QueueId);

            if (queue == null)
                return CommandResult.Failure("Queue not found", command.CorrelationId);

            if (command.Amount <= 0)
                return CommandResult.Failure("Invalid payment amount", command.CorrelationId);

            // TODO: Integrate with actual payment processor
            // For now, simulate payment validation
            return CommandResult.Ok(command.CorrelationId, "Payment validated successfully");
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Payment validation failed: {ex.Message}", command.CorrelationId);
        }
    }
}
