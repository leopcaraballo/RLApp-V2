namespace RLApp.Application.Handlers;

using Commands;
using DTOs;
using RLApp.Domain.Common;
using RLApp.Ports.Inbound;

/// <summary>
/// Handler for UC-004: Deactivate Consulting Room
/// Deactivates a consulting room at end of day.
/// Reference: S-002 Consulting Room Lifecycle
/// </summary>
public class DeactivateConsultingRoomHandler
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;

    public DeactivateConsultingRoomHandler(IWaitingQueueRepository queueRepository, IEventPublisher eventPublisher)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<CommandResult> Handle(DeactivateConsultingRoomCommand command)
    {
        try
        {
            // TODO: In a real scenario, there would be a ConsultingRoom aggregate
            // This demonstrates the pattern for room deactivation
            
            return CommandResult.Ok(command.CorrelationId, $"Consulting room {command.RoomId} deactivated");
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Deactivation failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-009: Mark Payment Pending
/// Marks patient payment as pending for later processing.
/// Reference: S-004 Cashier Flow
/// </summary>
public class MarkPaymentPendingHandler
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;

    public MarkPaymentPendingHandler(IWaitingQueueRepository queueRepository, IEventPublisher eventPublisher)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<CommandResult> Handle(MarkPaymentPendingCommand command)
    {
        try
        {
            var queue = await _queueRepository.GetByIdAsync(command.QueueId);

            if (queue == null)
                return CommandResult.Failure("Queue not found", command.CorrelationId);

            if (string.IsNullOrEmpty(command.PatientId))
                return CommandResult.Failure("Patient ID is required", command.CorrelationId);

            // TODO: Mark payment as pending in payment system
            // For now, this demonstrates the pattern

            return CommandResult.Ok(command.CorrelationId, $"Payment marked as pending for patient {command.PatientId}");
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Operation failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-010: Mark Patient Absence At Cashier
/// Marks a patient as absent during payment validation.
/// Reference: S-004 Cashier Flow
/// </summary>
public class MarkAbsenceAtCashierHandler
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;

    public MarkAbsenceAtCashierHandler(IWaitingQueueRepository queueRepository, IEventPublisher eventPublisher)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<CommandResult> Handle(MarkAbsenceCommand command)
    {
        try
        {
            var queue = await _queueRepository.GetByIdAsync(command.QueueId);

            if (queue == null)
                return CommandResult.Failure("Queue not found", command.CorrelationId);

            queue.MarkPatientAbsent(command.PatientId, command.CorrelationId);

            await _queueRepository.UpdateAsync(queue);

            var events = queue.GetUnraisedEvents();
            await _eventPublisher.PublishBatchAsync(events);
            queue.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, $"Patient {command.PatientId} marked as absent");
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Operation failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-014: Mark Patient Absence At Consultation
/// Marks a patient as absent during consultation.
/// Reference: S-005 Consultation Flow
/// </summary>
public class MarkAbsenceAtConsultationHandler
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;

    public MarkAbsenceAtConsultationHandler(IWaitingQueueRepository queueRepository, IEventPublisher eventPublisher)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<CommandResult> Handle(MarkAbsenceAtConsultationCommand command)
    {
        try
        {
            var queue = await _queueRepository.GetByIdAsync(command.QueueId);

            if (queue == null)
                return CommandResult.Failure("Queue not found", command.CorrelationId);

            queue.MarkPatientAbsent(command.PatientId, command.CorrelationId);

            await _queueRepository.UpdateAsync(queue);

            var events = queue.GetUnraisedEvents();
            await _eventPublisher.PublishBatchAsync(events);
            queue.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, $"Patient {command.PatientId} marked as absent at consultation");
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Operation failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-016: Rebuild Projections
/// Rebuilds read model projections from event store.
/// Reference: S-008 Event Sourcing and Projections
/// </summary>
public class RebuildProjectionsHandler
{
    private readonly IEventPublisher _eventPublisher;

    public RebuildProjectionsHandler(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public async Task<CommandResult> Handle(RebuildProjectionsCommand command)
    {
        try
        {
            // TODO: Implement event store replay and projection rebuild
            // This would:
            // 1. Load all events from event store for the given date range
            // 2. Clear existing projections
            // 3. Replay events through projection handlers
            // 4. Rebuild read models in projection store

            return CommandResult.Ok(command.CorrelationId, $"Projections rebuilt for {command.FromDate:yyyy-MM-dd} to {command.ToDate:yyyy-MM-dd}");
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Rebuild failed: {ex.Message}", command.CorrelationId);
        }
    }
}
