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
/// Handler for UC-011: Claim Next Patient For Consultation
/// Claims next patient for medical consultation.
/// Reference: S-005 Consultation Flow
/// </summary>
public class ClaimNextPatientHandler : IRequestHandler<ClaimNextPatientCommand, CommandResult<ClaimedPatientResultDto>>
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IConsultingRoomRepository _roomRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;

    public ClaimNextPatientHandler(
        IWaitingQueueRepository queueRepository,
        IConsultingRoomRepository roomRepository,
        IEventPublisher eventPublisher,
        IAuditStore auditStore,
        IPersistenceSession persistenceSession)
    {
        _queueRepository = queueRepository;
        _roomRepository = roomRepository;
        _eventPublisher = eventPublisher;
        _auditStore = auditStore;
        _persistenceSession = persistenceSession;
    }

    public async Task<CommandResult<ClaimedPatientResultDto>> Handle(ClaimNextPatientCommand command, CancellationToken cancellationToken)
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
                    "CLAIM_NEXT_PATIENT",
                    "WaitingQueue",
                    command.QueueId,
                    new { command.QueueId, command.RoomId },
                    command.CorrelationId,
                    "Queue not found",
                    cancellationToken);
                return CommandResult<ClaimedPatientResultDto>.Failure("Queue not found", command.CorrelationId);
            }

            var patientId = queue.GetNextPatient();

            queue.AssignPatientToRoom(patientId, command.RoomId, command.CorrelationId);
            await _queueRepository.UpdateAsync(queue, cancellationToken);

            var room = await _roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
            room.AssignPatient(patientId, command.UserId, command.CorrelationId);
            await _roomRepository.UpdateAsync(room, cancellationToken);

            await _eventPublisher.PublishBatchAsync(queue.GetUnraisedEvents(), cancellationToken);
            await _eventPublisher.PublishBatchAsync(room.GetUnraisedEvents(), cancellationToken);

            var result = new ClaimedPatientResultDto
            {
                QueueId = targetQueueId,
                TurnId = TurnReferenceParser.Build(targetQueueId, patientId),
                TurnNumber = TurnReferenceParser.Build(targetQueueId, patientId),
                PatientId = patientId,
                ConsultingRoomId = command.RoomId,
                CurrentState = OperationalVisibleStatuses.WaitingForConsultation,
                ClaimStatus = "Claimed",
                CorrelationId = command.CorrelationId,
                ClaimedAt = DateTime.UtcNow
            };

            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "CLAIM_NEXT_PATIENT",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, PatientId = patientId, command.RoomId },
                command.CorrelationId,
                cancellationToken);

            queue.ClearUnraisedEvents();
            room.ClearUnraisedEvents();

            return CommandResult<ClaimedPatientResultDto>.Ok(result, command.CorrelationId, "Patient claimed successfully");
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "CLAIM_NEXT_PATIENT",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.RoomId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult<ClaimedPatientResultDto>.Failure(ex, command.CorrelationId);
        }
        catch (Exception ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "CLAIM_NEXT_PATIENT",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.RoomId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult<ClaimedPatientResultDto>.Failure($"Claim failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-011: Medical Call Next
/// Claims and calls the next patient from the medical console.
/// Reference: S-005 Consultation Flow
/// </summary>
public class MedicalCallNextHandler : IRequestHandler<MedicalCallNextCommand, CommandResult<PatientCallResultDto>>
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IConsultingRoomRepository _roomRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;
    private readonly PatientTrajectoryCorrelationResolver _trajectoryCorrelationResolver;

    public MedicalCallNextHandler(
        IWaitingQueueRepository queueRepository,
        IConsultingRoomRepository roomRepository,
        IEventPublisher eventPublisher,
        IAuditStore auditStore,
        IPersistenceSession persistenceSession,
        PatientTrajectoryCorrelationResolver trajectoryCorrelationResolver)
    {
        _queueRepository = queueRepository;
        _roomRepository = roomRepository;
        _eventPublisher = eventPublisher;
        _auditStore = auditStore;
        _persistenceSession = persistenceSession;
        _trajectoryCorrelationResolver = trajectoryCorrelationResolver;
    }

    public async Task<CommandResult<PatientCallResultDto>> Handle(MedicalCallNextCommand command, CancellationToken cancellationToken)
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
                    "MEDICAL_CALL_NEXT",
                    "WaitingQueue",
                    command.QueueId,
                    new { command.QueueId, command.RoomId },
                    command.CorrelationId,
                    "Queue not found",
                    cancellationToken);
                return CommandResult<PatientCallResultDto>.Failure("Queue not found", command.CorrelationId);
            }

            var patientId = queue.GetNextPatient();
            queue.AssignPatientToRoom(patientId, command.RoomId, command.CorrelationId);
            var trajectoryId = await _trajectoryCorrelationResolver.ResolveRequiredAsync(patientId, targetQueueId, cancellationToken);
            queue.CallPatient(patientId, command.RoomId, command.CorrelationId, trajectoryId);
            await _queueRepository.UpdateAsync(queue, cancellationToken);

            var room = await _roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
            room.AssignPatient(patientId, command.UserId, command.CorrelationId);
            await _roomRepository.UpdateAsync(room, cancellationToken);

            await _eventPublisher.PublishBatchAsync(queue.GetUnraisedEvents(), cancellationToken);
            await _eventPublisher.PublishBatchAsync(room.GetUnraisedEvents(), cancellationToken);

            var turnId = TurnReferenceParser.Build(targetQueueId, patientId);
            var result = new PatientCallResultDto
            {
                TurnId = turnId,
                TurnNumber = turnId,
                PatientId = patientId,
                CurrentState = OperationalVisibleStatuses.Called,
                ConsultingRoomId = command.RoomId,
                CorrelationId = command.CorrelationId,
                CalledAt = DateTime.UtcNow,
                QueuePosition = 1
            };

            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "MEDICAL_CALL_NEXT",
                "ConsultingRoom",
                command.RoomId,
                new { command.QueueId, PatientId = patientId, command.RoomId },
                command.CorrelationId,
                cancellationToken);

            queue.ClearUnraisedEvents();
            room.ClearUnraisedEvents();

            return CommandResult<PatientCallResultDto>.Ok(result, command.CorrelationId, "Patient called successfully");
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "MEDICAL_CALL_NEXT",
                "ConsultingRoom",
                command.RoomId,
                new { command.QueueId, command.RoomId },
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
                "MEDICAL_CALL_NEXT",
                "ConsultingRoom",
                command.RoomId,
                new { command.QueueId, command.RoomId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult<PatientCallResultDto>.Failure($"Medical call-next failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-012: Call Patient To Consultation
/// Calls patient to consultation room.
/// Reference: S-005 Consultation Flow
/// </summary>
public class CallPatientToConsultationHandler : IRequestHandler<CallPatientCommand, CommandResult>
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;
    private readonly PatientTrajectoryCorrelationResolver _trajectoryCorrelationResolver;

    public CallPatientToConsultationHandler(
        IWaitingQueueRepository queueRepository,
        IEventPublisher eventPublisher,
        IAuditStore auditStore,
        IPersistenceSession persistenceSession,
        PatientTrajectoryCorrelationResolver trajectoryCorrelationResolver)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
        _auditStore = auditStore;
        _persistenceSession = persistenceSession;
        _trajectoryCorrelationResolver = trajectoryCorrelationResolver;
    }

    public async Task<CommandResult> Handle(CallPatientCommand command, CancellationToken cancellationToken)
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
                    "CALL_PATIENT_TO_CONSULTATION",
                    "WaitingQueue",
                    command.QueueId,
                    new { command.QueueId, command.PatientId, command.RoomId },
                    command.CorrelationId,
                    "Queue not found",
                    cancellationToken);
                return CommandResult.Failure("Queue not found", command.CorrelationId);
            }

            var trajectoryId = await _trajectoryCorrelationResolver.ResolveRequiredAsync(
                command.PatientId,
                targetQueueId,
                cancellationToken);

            queue.CallPatient(command.PatientId, command.RoomId, command.CorrelationId, trajectoryId);
            await _queueRepository.UpdateAsync(queue, cancellationToken);

            var events = queue.GetUnraisedEvents();
            await _eventPublisher.PublishBatchAsync(events, cancellationToken);
            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "CALL_PATIENT_TO_CONSULTATION",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId, command.RoomId },
                command.CorrelationId,
                cancellationToken);
            queue.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, "Patient called to consultation");
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "CALL_PATIENT_TO_CONSULTATION",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId, command.RoomId },
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
                "CALL_PATIENT_TO_CONSULTATION",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId, command.RoomId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure($"Call failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-012: Start Consultation
/// Marks the called patient as actively in consultation.
/// Reference: S-005 Consultation Flow
/// </summary>
public class StartConsultationHandler : IRequestHandler<StartConsultationCommand, CommandResult>
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IConsultingRoomRepository _roomRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;
    private readonly PatientTrajectoryOrchestrator _trajectoryOrchestrator;
    private readonly PatientTrajectoryCorrelationResolver _trajectoryCorrelationResolver;

    public StartConsultationHandler(
        IWaitingQueueRepository queueRepository,
        IConsultingRoomRepository roomRepository,
        IEventPublisher eventPublisher,
        IAuditStore auditStore,
        IPersistenceSession persistenceSession,
        PatientTrajectoryOrchestrator trajectoryOrchestrator,
        PatientTrajectoryCorrelationResolver trajectoryCorrelationResolver)
    {
        _queueRepository = queueRepository;
        _roomRepository = roomRepository;
        _eventPublisher = eventPublisher;
        _auditStore = auditStore;
        _persistenceSession = persistenceSession;
        _trajectoryOrchestrator = trajectoryOrchestrator;
        _trajectoryCorrelationResolver = trajectoryCorrelationResolver;
    }

    public async Task<CommandResult> Handle(StartConsultationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var room = await _roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
            if (string.IsNullOrWhiteSpace(room.CurrentPatientId))
            {
                return CommandResult.Failure("No patient is currently assigned to this consulting room", command.CorrelationId);
            }

            if (!TurnReferenceParser.TryExtractQueueId(command.TurnId, room.CurrentPatientId, out var queueId))
            {
                return CommandResult.Failure("Turn ID does not match the patient currently assigned to the consulting room", command.CorrelationId);
            }

            var queue = await _queueRepository.GetByIdAsync(queueId, cancellationToken);
            var trajectoryId = await _trajectoryCorrelationResolver.ResolveRequiredAsync(room.CurrentPatientId, queueId, cancellationToken);

            queue.StartPatientAttention(room.CurrentPatientId, command.RoomId, command.CorrelationId, trajectoryId);
            await _queueRepository.UpdateAsync(queue, cancellationToken);

            var queueEvents = queue.GetUnraisedEvents();
            var startedEvent = queueEvents.OfType<RLApp.Domain.Events.PatientClaimedForAttention>().Last();
            await _trajectoryOrchestrator.TrackConsultationStartedAsync(queueId, startedEvent, cancellationToken);
            await _eventPublisher.PublishBatchAsync(queueEvents, cancellationToken);

            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "START_CONSULTATION",
                "ConsultingRoom",
                command.RoomId,
                new { command.TurnId, command.RoomId, PatientId = room.CurrentPatientId, QueueId = queueId },
                command.CorrelationId,
                cancellationToken);

            queue.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, "Consultation started");
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "START_CONSULTATION",
                "ConsultingRoom",
                command.RoomId,
                new { command.TurnId, command.RoomId },
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
                "START_CONSULTATION",
                "ConsultingRoom",
                command.RoomId,
                new { command.TurnId, command.RoomId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure($"Start consultation failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-013: Finish Consultation
/// Marks consultation as completed and releases patient from queue.
/// Reference: S-005 Consultation Flow
/// </summary>
public class FinishConsultationHandler : IRequestHandler<FinishConsultationCommand, CommandResult>
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IConsultingRoomRepository _roomRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;
    private readonly PatientTrajectoryOrchestrator _trajectoryOrchestrator;
    private readonly PatientTrajectoryCorrelationResolver _trajectoryCorrelationResolver;

    public FinishConsultationHandler(
        IWaitingQueueRepository queueRepository,
        IConsultingRoomRepository roomRepository,
        IEventPublisher eventPublisher,
        IAuditStore auditStore,
        IPersistenceSession persistenceSession,
        PatientTrajectoryOrchestrator trajectoryOrchestrator,
        PatientTrajectoryCorrelationResolver trajectoryCorrelationResolver)
    {
        _queueRepository = queueRepository;
        _roomRepository = roomRepository;
        _eventPublisher = eventPublisher;
        _auditStore = auditStore;
        _persistenceSession = persistenceSession;
        _trajectoryOrchestrator = trajectoryOrchestrator;
        _trajectoryCorrelationResolver = trajectoryCorrelationResolver;
    }

    public async Task<CommandResult> Handle(FinishConsultationCommand command, CancellationToken cancellationToken)
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
                    "FINISH_CONSULTATION",
                    "WaitingQueue",
                    command.QueueId,
                    new { command.QueueId, command.PatientId, command.RoomId },
                    command.CorrelationId,
                    "Queue not found",
                    cancellationToken);
                return CommandResult.Failure("Queue not found", command.CorrelationId);
            }

            var trajectoryId = await _trajectoryCorrelationResolver.ResolveRequiredAsync(
                command.PatientId,
                targetQueueId,
                cancellationToken);

            queue.CompletePatientAttention(
                command.PatientId,
                command.RoomId,
                command.TurnId,
                command.Outcome,
                command.CorrelationId,
                trajectoryId);
            await _queueRepository.UpdateAsync(queue, cancellationToken);

            var room = await _roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
            room.CompleteAttention(command.TurnId, command.Outcome, command.CorrelationId, trajectoryId);
            await _roomRepository.UpdateAsync(room, cancellationToken);

            var queueEvents = queue.GetUnraisedEvents();
            var completionEvent = queueEvents.OfType<RLApp.Domain.Events.PatientAttentionCompleted>().Last();
            await _trajectoryOrchestrator.TrackCompletionAsync(targetQueueId, completionEvent, cancellationToken);

            await _eventPublisher.PublishBatchAsync(queueEvents, cancellationToken);
            await _eventPublisher.PublishBatchAsync(room.GetUnraisedEvents(), cancellationToken);
            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "FINISH_CONSULTATION",
                "ConsultingRoom",
                command.RoomId,
                new { command.QueueId, command.PatientId, command.RoomId },
                command.CorrelationId,
                cancellationToken);

            queue.ClearUnraisedEvents();
            room.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, "Consultation completed");
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "FINISH_CONSULTATION",
                "ConsultingRoom",
                command.RoomId,
                new { command.QueueId, command.PatientId, command.RoomId },
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
                "FINISH_CONSULTATION",
                "ConsultingRoom",
                command.RoomId,
                new { command.QueueId, command.PatientId, command.RoomId },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure($"Completion failed: {ex.Message}", command.CorrelationId);
        }
    }
}
