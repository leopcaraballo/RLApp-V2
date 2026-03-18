namespace RLApp.Application.Handlers;

using Commands;
using DTOs;
using RLApp.Domain.Common;
using RLApp.Ports.Inbound;

/// <summary>
/// Handler for UC-011: Claim Next Patient For Consultation
/// Claims next patient for medical consultation.
/// Reference: S-005 Consultation Flow
/// </summary>
public class ClaimNextPatientHandler
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;

    public ClaimNextPatientHandler(IWaitingQueueRepository queueRepository, IEventPublisher eventPublisher)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<CommandResult<ClaimedPatientResultDto>> Handle(ClaimNextPatientCommand command)
    {
        try
        {
            var queue = await _queueRepository.GetByIdAsync(command.QueueId);

            if (queue == null)
                return CommandResult<ClaimedPatientResultDto>.Failure("Queue not found", command.CorrelationId);

            var patientId = queue.GetNextPatient();

            queue.AssignPatientToRoom(patientId, command.RoomId, command.CorrelationId);

            await _queueRepository.UpdateAsync(queue);

            var events = queue.GetUnraisedEvents();
            await _eventPublisher.PublishBatchAsync(events);
            queue.ClearUnraisedEvents();

            var result = new ClaimedPatientResultDto
            {
                PatientId = patientId,
                RoomId = command.RoomId,
                ClaimedAt = DateTime.UtcNow
            };

            return CommandResult<ClaimedPatientResultDto>.Ok(result, command.CorrelationId, "Patient claimed successfully");
        }
        catch (DomainException ex)
        {
            return CommandResult<ClaimedPatientResultDto>.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            return CommandResult<ClaimedPatientResultDto>.Failure($"Claim failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// DTO for claimed patient result.
/// </summary>
public class ClaimedPatientResultDto
{
    public string PatientId { get; set; }
    public string RoomId { get; set; }
    public DateTime ClaimedAt { get; set; }
}

/// <summary>
/// Handler for UC-012: Call Patient To Consultation
/// Calls patient to consultation room.
/// Reference: S-005 Consultation Flow
/// </summary>
public class CallPatientToConsultationHandler
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;

    public CallPatientToConsultationHandler(IWaitingQueueRepository queueRepository, IEventPublisher eventPublisher)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<CommandResult> Handle(CallPatientCommand command)
    {
        try
        {
            var queue = await _queueRepository.GetByIdAsync(command.QueueId);

            if (queue == null)
                return CommandResult.Failure("Queue not found", command.CorrelationId);

            queue.CallPatient(command.PatientId, command.RoomId, command.CorrelationId);

            await _queueRepository.UpdateAsync(queue);

            var events = queue.GetUnraisedEvents();
            await _eventPublisher.PublishBatchAsync(events);
            queue.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, "Patient called to consultation");
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Call failed: {ex.Message}", command.CorrelationId);
        }
    }
}

/// <summary>
/// Handler for UC-013: Finish Consultation
/// Marks consultation as completed and releases patient from queue.
/// Reference: S-005 Consultation Flow
/// </summary>
public class FinishConsultationHandler
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;

    public FinishConsultationHandler(IWaitingQueueRepository queueRepository, IEventPublisher eventPublisher)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<CommandResult> Handle(FinishConsultationCommand command)
    {
        try
        {
            var queue = await _queueRepository.GetByIdAsync(command.QueueId);

            if (queue == null)
                return CommandResult.Failure("Queue not found", command.CorrelationId);

            queue.CompletePatientAttention(command.PatientId, command.RoomId, command.CorrelationId);

            await _queueRepository.UpdateAsync(queue);

            var events = queue.GetUnraisedEvents();
            await _eventPublisher.PublishBatchAsync(events);
            queue.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, "Consultation completed");
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Completion failed: {ex.Message}", command.CorrelationId);
        }
    }
}
