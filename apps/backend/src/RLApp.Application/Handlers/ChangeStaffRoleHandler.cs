namespace RLApp.Application.Handlers;

using Commands;
using DTOs;
using MediatR;
using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.ValueObjects;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

/// <summary>
/// Handler for UC-002: Manage Internal Roles
/// Changes staff role with full traceability.
/// Reference: S-001 Staff Identity And Access
/// </summary>
public class ChangeStaffRoleHandler : IRequestHandler<ChangeStaffRoleCommand, CommandResult>
{
    private readonly IStaffUserRepository _staffUserRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;

    public ChangeStaffRoleHandler(
        IStaffUserRepository staffUserRepository,
        IEventPublisher eventPublisher,
        IAuditStore auditStore,
        IPersistenceSession persistenceSession)
    {
        _staffUserRepository = staffUserRepository;
        _eventPublisher = eventPublisher;
        _auditStore = auditStore;
        _persistenceSession = persistenceSession;
    }

    public async Task<CommandResult> Handle(ChangeStaffRoleCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var staffUser = await _staffUserRepository.GetByIdAsync(command.StaffId, cancellationToken);

            if (staffUser == null)
            {
                await HandlerPersistence.CommitFailureAsync(
                    _persistenceSession,
                    _auditStore,
                    command.UserId,
                    "CHANGE_ROLE",
                    "StaffUser",
                    command.StaffId,
                    new { command.StaffId, command.NewRole },
                    command.CorrelationId,
                    "Staff user not found",
                    cancellationToken);
                return CommandResult.Failure("Staff user not found", command.CorrelationId);
            }

            var newRole = StaffRole.Create(command.NewRole);
            staffUser.ChangeRole(newRole, command.Reason, command.CorrelationId);

            await _staffUserRepository.UpdateAsync(staffUser, cancellationToken);

            // Publish domain events
            var events = staffUser.GetUnraisedEvents();
            await _eventPublisher.PublishBatchAsync(events, cancellationToken);
            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "CHANGE_ROLE",
                "StaffUser",
                command.StaffId,
                new { command.StaffId, NewRole = newRole.ToString() },
                command.CorrelationId,
                cancellationToken);
            staffUser.ClearUnraisedEvents();

            return CommandResult.Ok(command.CorrelationId, $"Role changed to {newRole}");
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "CHANGE_ROLE",
                "StaffUser",
                command.StaffId,
                new { command.StaffId, command.NewRole },
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
                "CHANGE_ROLE",
                "StaffUser",
                command.StaffId,
                new { command.StaffId, command.NewRole },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult.Failure($"Operation failed: {ex.Message}", command.CorrelationId);
        }
    }
}
