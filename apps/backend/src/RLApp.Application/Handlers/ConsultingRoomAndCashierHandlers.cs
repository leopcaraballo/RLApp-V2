namespace RLApp.Application.Handlers;

using Commands;
using DTOs;
using MediatR;
using RLApp.Application.Services;
using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

/// <summary>
/// Handler for UC-003: Activate Consulting Room
/// Activates a consulting room for daily operations.
/// Reference: S-002 Consulting Room Lifecycle
/// </summary>
public class ActivateConsultingRoomHandler : IRequestHandler<ActivateConsultingRoomCommand, CommandResult>
{
    private readonly IConsultingRoomRepository _consultingRoomRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;

    public ActivateConsultingRoomHandler(
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

    public async Task<CommandResult> Handle(ActivateConsultingRoomCommand command, CancellationToken cancellationToken)
    {
        try
        {
            ConsultingRoom room;

            try
            {
                room = await _consultingRoomRepository.GetByIdAsync(command.RoomId, cancellationToken);
                room.Activate(command.CorrelationId);
                await _consultingRoomRepository.UpdateAsync(room, cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                room = ConsultingRoom.Create(command.RoomId, command.RoomName, command.CorrelationId);
                await _consultingRoomRepository.AddAsync(room, cancellationToken);
            }

            var events = room.GetUnraisedEvents();
            await _eventPublisher.PublishBatchAsync(events, cancellationToken);
            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "ACTIVATE_CONSULTING_ROOM",
                "ConsultingRoom",
                command.RoomId,
                new { command.RoomId, command.RoomName },
                command.CorrelationId,
                cancellationToken);
            room.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, $"Consulting room {command.RoomName} activated");
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "ACTIVATE_CONSULTING_ROOM",
                "ConsultingRoom",
                command.RoomId,
                new { command.RoomId, command.RoomName },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure(ex, command.CorrelationId);
        }
        catch (Exception ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "ACTIVATE_CONSULTING_ROOM",
                "ConsultingRoom",
                command.RoomId,
                new { command.RoomId, command.RoomName },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure($"Activation failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-007: Call Next At Cashier
/// Calls next patient at cashier for payment validation.
/// Reference: S-004 Cashier Flow
/// </summary>
public class CallNextAtCashierHandler : IRequestHandler<CallNextAtCashierCommand, CommandResult<PatientCallResultDto>>
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;

    public CallNextAtCashierHandler(
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

    public async Task<CommandResult<PatientCallResultDto>> Handle(CallNextAtCashierCommand command, CancellationToken cancellationToken)
    {
        try
        {
            WaitingQueue queue;
            string targetQueueId = string.IsNullOrWhiteSpace(command.QueueId) ? "MAIN-QUEUE-001" : command.QueueId;
            try
            {
                queue = await _queueRepository.GetByIdAsync(targetQueueId, cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                await HandlerPersistence.CommitFailureAsync(
                    _persistenceSession,
                    _auditStore,
                    command.UserId,
                    "CALL_NEXT_AT_CASHIER",
                    "WaitingQueue",
                    command.QueueId,
                    new { command.QueueId },
                    command.CorrelationId,
                    "Queue not found",
                    cancellationToken);
                return CommandResult<PatientCallResultDto>.Failure("Queue not found", command.CorrelationId);
            }

            var patientId = queue.GetNextPatient();

            queue.CallPatientAtCashier(patientId, command.CashierStationId, command.CorrelationId);

            await _queueRepository.UpdateAsync(queue, cancellationToken);

            var events = queue.GetUnraisedEvents();
            await _eventPublisher.PublishBatchAsync(events, cancellationToken);

            var result = new PatientCallResultDto
            {
                TurnId = TurnReferenceParser.Build(targetQueueId, patientId),
                TurnNumber = TurnReferenceParser.Build(targetQueueId, patientId),
                PatientId = patientId,
                CurrentState = OperationalVisibleStatuses.AtCashier,
                CashierStationId = command.CashierStationId,
                CorrelationId = command.CorrelationId,
                CalledAt = DateTime.UtcNow,
                QueuePosition = 1
            };

            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "CALL_NEXT_AT_CASHIER",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, PatientId = patientId },
                command.CorrelationId,
                cancellationToken);
            queue.ClearUnraisedEvents();

            return CommandResult<PatientCallResultDto>.Ok(result, command.CorrelationId, "Patient called successfully");
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "CALL_NEXT_AT_CASHIER",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult<PatientCallResultDto>.Failure(ex, command.CorrelationId);
        }
        catch (Exception ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "CALL_NEXT_AT_CASHIER",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult<PatientCallResultDto>.Failure($"Call failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-008: Validate Payment
/// Validates patient payment.
/// Reference: S-004 Cashier Flow
/// </summary>
public class ValidatePaymentHandler : IRequestHandler<ValidatePaymentCommand, CommandResult>
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;
    private readonly PatientTrajectoryOrchestrator _trajectoryOrchestrator;

    public ValidatePaymentHandler(
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

    public async Task<CommandResult> Handle(ValidatePaymentCommand command, CancellationToken cancellationToken)
    {
        try
        {
            WaitingQueue queue;
            string targetQueueId = string.IsNullOrWhiteSpace(command.QueueId) ? "MAIN-QUEUE-001" : command.QueueId;
            try
            {
                queue = await _queueRepository.GetByIdAsync(targetQueueId, cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                await HandlerPersistence.CommitFailureAsync(
                    _persistenceSession,
                    _auditStore,
                    command.UserId,
                    "VALIDATE_PAYMENT",
                    "WaitingQueue",
                    command.QueueId,
                    new { command.QueueId, command.PatientId, command.Amount },
                    command.CorrelationId,
                    "Queue not found",
                    cancellationToken);
                return CommandResult.Failure("Queue not found", command.CorrelationId);
            }

            if (command.Amount <= 0)
            {
                await HandlerPersistence.CommitFailureAsync(
                    _persistenceSession,
                    _auditStore,
                    command.UserId,
                    "VALIDATE_PAYMENT",
                    "WaitingQueue",
                    command.QueueId,
                    new { command.QueueId, command.PatientId, command.Amount },
                    command.CorrelationId,
                    "Invalid payment amount",
                    cancellationToken);
                return CommandResult.Failure("Invalid payment amount", command.CorrelationId);
            }

            queue.MarkPaymentValidated(
                command.PatientId,
                command.Amount,
                command.TurnId,
                command.PaymentReference,
                command.CorrelationId);
            await _queueRepository.UpdateAsync(queue, cancellationToken);

            var events = queue.GetUnraisedEvents();
            var paymentValidatedEvent = events.OfType<RLApp.Domain.Events.PatientPaymentValidated>().Last();
            await _trajectoryOrchestrator.TrackPaymentValidatedAsync(targetQueueId, paymentValidatedEvent, cancellationToken);
            await _eventPublisher.PublishBatchAsync(events, cancellationToken);
            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "VALIDATE_PAYMENT",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId, command.Amount },
                command.CorrelationId,
                cancellationToken);
            queue.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, "Payment validated successfully");
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "VALIDATE_PAYMENT",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId, command.Amount },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure(ex, command.CorrelationId);
        }
        catch (Exception ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "VALIDATE_PAYMENT",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId, command.Amount },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure($"Payment validation failed: {ex.Message}", command.CorrelationId);
        }
    }
}
