namespace RLApp.Application.Handlers;

using Commands;
using DTOs;
using MediatR;
using RLApp.Application.Services;
using RLApp.Domain.Common;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

/// <summary>
/// Handler for UC-004: Deactivate Consulting Room
/// Deactivates a consulting room at end of day.
/// Reference: S-002 Consulting Room Lifecycle
/// </summary>
public class DeactivateConsultingRoomHandler : IRequestHandler<DeactivateConsultingRoomCommand, CommandResult>
{
    private readonly IConsultingRoomRepository _consultingRoomRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;

    public DeactivateConsultingRoomHandler(
        IConsultingRoomRepository consultingRoomRepository,
        IEventPublisher eventPublisher,
        IAuditStore auditStore,
        IPersistenceSession persistenceSession)
    {
        _consultingRoomRepository = consultingRoomRepository;
        _eventPublisher = eventPublisher;
        _auditStore = auditStore;
        _persistenceSession = persistenceSession;
    }

    public async Task<CommandResult> Handle(DeactivateConsultingRoomCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var room = await _consultingRoomRepository.GetByIdAsync(command.RoomId, cancellationToken);
            room.Deactivate(command.CorrelationId);
            await _consultingRoomRepository.UpdateAsync(room, cancellationToken);

            var events = room.GetUnraisedEvents();
            await _eventPublisher.PublishBatchAsync(events, cancellationToken);
            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "DEACTIVATE_CONSULTING_ROOM",
                "ConsultingRoom",
                command.RoomId,
                new { command.RoomId },
                command.CorrelationId,
                cancellationToken);
            room.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, $"Consulting room {command.RoomId} deactivated");
        }
        catch (KeyNotFoundException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "DEACTIVATE_CONSULTING_ROOM",
                "ConsultingRoom",
                command.RoomId,
                new { command.RoomId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.NotFound($"Room not found: {ex.Message}", command.CorrelationId);
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "DEACTIVATE_CONSULTING_ROOM",
                "ConsultingRoom",
                command.RoomId,
                new { command.RoomId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "DEACTIVATE_CONSULTING_ROOM",
                "ConsultingRoom",
                command.RoomId,
                new { command.RoomId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure($"Deactivation failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-009: Mark Payment Pending
/// Marks patient payment as pending for later processing.
/// Reference: S-004 Cashier Flow
/// </summary>
public class MarkPaymentPendingHandler : IRequestHandler<MarkPaymentPendingCommand, CommandResult>
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;

    public MarkPaymentPendingHandler(
        IWaitingQueueRepository queueRepository,
        IEventPublisher eventPublisher,
        IAuditStore auditStore,
        IPersistenceSession persistenceSession)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
        _auditStore = auditStore;
        _persistenceSession = persistenceSession;
    }

    public async Task<CommandResult> Handle(MarkPaymentPendingCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var queue = await _queueRepository.GetByIdAsync(command.QueueId, cancellationToken);

            if (string.IsNullOrEmpty(command.PatientId))
            {
                await HandlerPersistence.CommitFailureAsync(
                    _persistenceSession,
                    _auditStore,
                    command.UserId,
                    "MARK_PAYMENT_PENDING",
                    "WaitingQueue",
                    command.QueueId,
                    new { command.QueueId, command.PatientId },
                    command.CorrelationId,
                    "Patient ID is required",
                    cancellationToken);
                return CommandResult.Failure("Patient ID is required", command.CorrelationId);
            }

            queue.MarkPaymentPending(command.PatientId, command.CorrelationId);
            await _queueRepository.UpdateAsync(queue, cancellationToken);

            var events = queue.GetUnraisedEvents();
            await _eventPublisher.PublishBatchAsync(events, cancellationToken);
            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "MARK_PAYMENT_PENDING",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId },
                command.CorrelationId,
                cancellationToken);
            queue.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, $"Payment marked as pending for patient {command.PatientId}");
        }
        catch (KeyNotFoundException)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "MARK_PAYMENT_PENDING",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId },
                command.CorrelationId,
                "Queue not found",
                cancellationToken);
            return CommandResult.Failure("Queue not found", command.CorrelationId);
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "MARK_PAYMENT_PENDING",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "MARK_PAYMENT_PENDING",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure($"Operation failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-010: Mark Patient Absence At Cashier
/// Marks a patient as absent during payment validation.
/// Reference: S-004 Cashier Flow
/// </summary>
public class MarkAbsenceAtCashierHandler : IRequestHandler<MarkAbsenceCommand, CommandResult>
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;
    private readonly PatientTrajectoryOrchestrator _trajectoryOrchestrator;

    public MarkAbsenceAtCashierHandler(
        IWaitingQueueRepository queueRepository,
        IEventPublisher eventPublisher,
        IAuditStore auditStore,
        IPersistenceSession persistenceSession,
        PatientTrajectoryOrchestrator trajectoryOrchestrator)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
        _auditStore = auditStore;
        _persistenceSession = persistenceSession;
        _trajectoryOrchestrator = trajectoryOrchestrator;
    }

    public async Task<CommandResult> Handle(MarkAbsenceCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var queue = await _queueRepository.GetByIdAsync(command.QueueId, cancellationToken);

            queue.MarkPatientAbsentAtCashier(command.PatientId, command.TurnId, command.Reason, command.CorrelationId);
            await _queueRepository.UpdateAsync(queue, cancellationToken);

            var events = queue.GetUnraisedEvents();
            var cashierAbsenceEvent = events.OfType<RLApp.Domain.Events.PatientAbsentAtCashier>().Last();
            await _trajectoryOrchestrator.TrackCashierAbsenceAsync(command.QueueId, cashierAbsenceEvent, cancellationToken);
            await _eventPublisher.PublishBatchAsync(events, cancellationToken);
            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "MARK_ABSENCE_AT_CASHIER",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId },
                command.CorrelationId,
                cancellationToken);
            queue.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, $"Patient {command.PatientId} marked as absent");
        }
        catch (KeyNotFoundException)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "MARK_ABSENCE_AT_CASHIER",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId },
                command.CorrelationId,
                "Queue not found",
                cancellationToken);
            return CommandResult.Failure("Queue not found", command.CorrelationId);
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "MARK_ABSENCE_AT_CASHIER",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "MARK_ABSENCE_AT_CASHIER",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure($"Operation failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-014: Mark Patient Absence At Consultation
/// Marks a patient as absent during consultation.
/// Reference: S-005 Consultation Flow
/// </summary>
public class MarkAbsenceAtConsultationHandler : IRequestHandler<MarkAbsenceAtConsultationCommand, CommandResult>
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IConsultingRoomRepository _roomRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;
    private readonly PatientTrajectoryOrchestrator _trajectoryOrchestrator;

    public MarkAbsenceAtConsultationHandler(
        IWaitingQueueRepository queueRepository,
        IConsultingRoomRepository roomRepository,
        IEventPublisher eventPublisher,
        IAuditStore auditStore,
        IPersistenceSession persistenceSession,
        PatientTrajectoryOrchestrator trajectoryOrchestrator)
    {
        _queueRepository = queueRepository;
        _roomRepository = roomRepository;
        _eventPublisher = eventPublisher;
        _auditStore = auditStore;
        _persistenceSession = persistenceSession;
        _trajectoryOrchestrator = trajectoryOrchestrator;
    }

    public async Task<CommandResult> Handle(MarkAbsenceAtConsultationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var queue = await _queueRepository.GetByIdAsync(command.QueueId, cancellationToken);

            queue.MarkPatientAbsent(command.PatientId, command.TurnId, command.Reason, command.CorrelationId);
            await _queueRepository.UpdateAsync(queue, cancellationToken);

            var room = await _roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
            room.MarkPatientAbsent(command.TurnId, command.Reason, command.CorrelationId);
            await _roomRepository.UpdateAsync(room, cancellationToken);

            var queueEvents = queue.GetUnraisedEvents();
            var consultationAbsenceEvent = queueEvents.OfType<RLApp.Domain.Events.PatientAbsentAtConsultation>().Last();
            await _trajectoryOrchestrator.TrackConsultationAbsenceAsync(command.QueueId, consultationAbsenceEvent, cancellationToken);

            await _eventPublisher.PublishBatchAsync(queueEvents, cancellationToken);
            await _eventPublisher.PublishBatchAsync(room.GetUnraisedEvents(), cancellationToken);
            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "MARK_ABSENCE_AT_CONSULTATION",
                "ConsultingRoom",
                command.RoomId,
                new { command.QueueId, command.PatientId, command.RoomId },
                command.CorrelationId,
                cancellationToken);

            queue.ClearUnraisedEvents();
            room.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, $"Patient {command.PatientId} marked as absent at consultation");
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "MARK_ABSENCE_AT_CONSULTATION",
                "ConsultingRoom",
                command.RoomId,
                new { command.QueueId, command.PatientId, command.RoomId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "MARK_ABSENCE_AT_CONSULTATION",
                "ConsultingRoom",
                command.RoomId,
                new { command.QueueId, command.PatientId, command.RoomId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure($"Operation failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-016: Rebuild Projections
/// Rebuilds read model projections from event store.
/// Reference: S-008 Event Sourcing and Projections
/// </summary>
public class RebuildProjectionsHandler : IRequestHandler<RebuildProjectionsCommand, CommandResult>
{
    private readonly IEventStore _eventStore;
    private readonly IProjectionStore _projectionStore;

    public RebuildProjectionsHandler(IEventStore eventStore, IProjectionStore projectionStore)
    {
        _eventStore = eventStore;
        _projectionStore = projectionStore;
    }

    public async Task<CommandResult> Handle(RebuildProjectionsCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var allEvents = await _eventStore.GetEventsByDateRangeAsync(
                command.FromDate, command.ToDate, cancellationToken);

            int processed = 0;
            foreach (var @event in allEvents.OrderBy(e => e.OccurredAt))
            {
                var projectionData = new Dictionary<string, object>
                {
                    ["EventType"] = @event.EventType,
                    ["AggregateId"] = @event.AggregateId,
                    ["OccurredAt"] = @event.OccurredAt
                };

                var projectionType = ResolveProjectionType(@event.EventType);
                if (projectionType != null)
                {
                    await _projectionStore.UpsertAsync(
                        @event.AggregateId, projectionType, projectionData, cancellationToken);
                    processed++;
                }
            }

            return CommandResult.Ok(
                command.CorrelationId,
                $"Projections rebuilt: {processed} events replayed for {command.FromDate:yyyy-MM-dd} to {command.ToDate:yyyy-MM-dd}");
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

    private static string? ResolveProjectionType(string eventType) => eventType switch
    {
        "QueueOpened" => "QueueState",
        "PatientCheckedIn" => "WaitingRoomMonitor",
        "PatientLeftWaiting" => "WaitingRoomMonitor",
        "ConsultantAssigned" => "WaitingRoomMonitor",
        "PatientCalledForConsultation" => "WaitingRoomMonitor",
        "ConsultationStarted" => "Dashboard",
        "ConsultationFinished" => "Dashboard",
        "PaymentProcessed" => "Dashboard",
        _ => null
    };
}
