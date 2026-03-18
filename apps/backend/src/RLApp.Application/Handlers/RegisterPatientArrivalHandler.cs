namespace RLApp.Application.Handlers;

using Commands;
using DTOs;
using RLApp.Domain.Common;
using RLApp.Ports.Inbound;

/// <summary>
/// Handler for UC-005: Register Patient Arrival
/// Checks in a patient and adds them to the queue.
/// Reference: S-003 Queue Open and Check-in
/// </summary>
public class RegisterPatientArrivalHandler
{
    private readonly IWaitingQueueRepository _queueRepository;
    private readonly IEventPublisher _eventPublisher;

    public RegisterPatientArrivalHandler(IWaitingQueueRepository queueRepository, IEventPublisher eventPublisher)
    {
        _queueRepository = queueRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<CommandResult> Handle(RegisterPatientArrivalCommand command)
    {
        try
        {
            var queue = await _queueRepository.GetByIdAsync(command.QueueId);

            if (queue == null)
                return CommandResult.Failure("Queue not found", command.CorrelationId);

            queue.CheckInPatient(command.PatientId, command.PatientName, command.CorrelationId);

            await _queueRepository.UpdateAsync(queue);

            // Publish domain events
            var events = queue.GetUnraisedEvents();
            await _eventPublisher.PublishBatchAsync(events);
            queue.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, "Patient checked in successfully");
        }
        catch (DomainException ex)
        {
            return CommandResult.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Check-in failed: {ex.Message}", command.CorrelationId);
        }
    }
}
