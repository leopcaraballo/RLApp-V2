namespace RLApp.Application.Handlers;

using Commands;
using DTOs;
using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.ValueObjects;
using RLApp.Ports.Inbound;

/// <summary>
/// Handler for UC-002: Manage Internal Roles
/// Changes staff role with full traceability.
/// Reference: S-001 Staff Identity And Access
/// </summary>
public class ChangeStaffRoleHandler
{
    private readonly IStaffUserRepository _staffUserRepository;
    private readonly IEventPublisher _eventPublisher;

    public ChangeStaffRoleHandler(IStaffUserRepository staffUserRepository, IEventPublisher eventPublisher)
    {
        _staffUserRepository = staffUserRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<CommandResult> Handle(ChangeStaffRoleCommand command)
    {
        try
        {
            var staffUser = await _staffUserRepository.GetByIdAsync(command.StaffId);

            if (staffUser == null)
                return CommandResult.Failure("Staff user not found", command.CorrelationId);

            var newRole = StaffRole.Create(command.NewRole);
            staffUser.ChangeRole(newRole);

            await _staffUserRepository.UpdateAsync(staffUser);

            // Publish domain events
            var events = staffUser.GetUnraisedEvents();
            await _eventPublisher.PublishBatchAsync(events);
            staffUser.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, $"Role changed to {newRole}");
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
