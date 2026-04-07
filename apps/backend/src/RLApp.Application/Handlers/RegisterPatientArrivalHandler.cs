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
/// Handler for UC-005: Register Patient Arrival
/// Checks in a patient and adds them to the queue.
/// Reference: S-003 Queue Open and Check-in
/// </summary>
public class RegisterPatientArrivalHandler : IRequestHandler<RegisterPatientArrivalCommand, CommandResult<RegisterPatientResultDto>>
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;
    private readonly PatientTrajectoryOrchestrator _trajectoryOrchestrator;
    private readonly IdempotencyGuard _idempotencyGuard;

    public RegisterPatientArrivalHandler(
        IWaitingQueueRepository queueRepository,
        IEventPublisher eventPublisher,
        IAuditStore auditStore,
        IPersistenceSession persistenceSession,
        PatientTrajectoryOrchestrator trajectoryOrchestrator,
        IdempotencyGuard idempotencyGuard)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
        _auditStore = auditStore;
        _persistenceSession = persistenceSession;
        _trajectoryOrchestrator = trajectoryOrchestrator;
        _idempotencyGuard = idempotencyGuard;
    }

    public async Task<CommandResult<RegisterPatientResultDto>> Handle(RegisterPatientArrivalCommand command, CancellationToken cancellationToken)
    {
        if (!_idempotencyGuard.TryAcquire(command.IdempotencyKey, command.CorrelationId))
        {
            return CommandResult<RegisterPatientResultDto>.Failure(
                "DUPLICATE_COMMAND",
                command.CorrelationId);
        }

        try
        {
            WaitingQueue queue;
            bool isNewQueue = false;

            string targetQueueId = string.IsNullOrWhiteSpace(command.QueueId) ? "MAIN-QUEUE-001" : command.QueueId;

            try
            {
                queue = await _queueRepository.GetByIdAsync(targetQueueId, cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                queue = WaitingQueue.Create(targetQueueId, "Main Waiting Room", command.CorrelationId);
                queue.Open();
                isNewQueue = true;
            }

            queue.CheckInPatient(
                command.PatientId,
                command.PatientName,
                command.AppointmentReference,
                command.Priority,
                command.Notes,
                command.CorrelationId);

            if (isNewQueue)
            {
                await _queueRepository.AddAsync(queue, cancellationToken);
            }
            else
            {
                await _queueRepository.UpdateAsync(queue, cancellationToken);
            }

            // Publish domain events
            var events = queue.GetUnraisedEvents();
            var checkedInEvent = events.OfType<RLApp.Domain.Events.PatientCheckedIn>().Last();
            await _trajectoryOrchestrator.TrackCheckInAsync(targetQueueId, checkedInEvent, cancellationToken);
            await _eventPublisher.PublishBatchAsync(events, cancellationToken);
            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "REGISTER_PATIENT_ARRIVAL",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId, command.PatientName, IsNewQueue = isNewQueue },
                command.CorrelationId,
                cancellationToken);
            queue.ClearUnraisedEvents();

            var result = new RegisterPatientResultDto
            {
                QueueId = targetQueueId,
                TurnId = $"{targetQueueId}-{command.PatientId}",
                PatientId = command.PatientId,
                RegisteredAt = DateTime.UtcNow
            };

            return CommandResult<RegisterPatientResultDto>.Ok(result, command.CorrelationId, "Patient checked in successfully");
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "REGISTER_PATIENT_ARRIVAL",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId, command.PatientName },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult<RegisterPatientResultDto>.Failure(ex, command.CorrelationId);
        }
        catch (Exception ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "REGISTER_PATIENT_ARRIVAL",
                "WaitingQueue",
                command.QueueId,
                new { command.QueueId, command.PatientId, command.PatientName },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult<RegisterPatientResultDto>.Failure($"Check-in failed: {ex.Message}", command.CorrelationId);
        }
        finally
        {
            _idempotencyGuard.Release(command.IdempotencyKey, command.CorrelationId);
        }
    }
}
