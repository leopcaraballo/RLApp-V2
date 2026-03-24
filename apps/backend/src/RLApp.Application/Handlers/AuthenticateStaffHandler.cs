namespace RLApp.Application.Handlers;

using Commands;
using DTOs;
using MediatR;
using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

/// <summary>
/// Handler for UC-001: Authenticate Staff
/// Validates credentials and returns authentication token metadata.
/// Reference: S-001 Staff Identity And Access
/// </summary>
public class AuthenticateStaffHandler : IRequestHandler<AuthenticateStaffCommand, CommandResult<AuthenticationResultDto>>
{
    private readonly IStaffUserRepository _staffUserRepository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;

    public AuthenticateStaffHandler(
        IStaffUserRepository staffUserRepository,
        IPasswordHashService passwordHashService,
        IJwtTokenService jwtTokenService,
        IAuditStore auditStore,
        IPersistenceSession persistenceSession)
    {
        _staffUserRepository = staffUserRepository;
        _passwordHashService = passwordHashService;
        _jwtTokenService = jwtTokenService;
        _auditStore = auditStore;
        _persistenceSession = persistenceSession;
    }

    public async Task<CommandResult<AuthenticationResultDto>> Handle(AuthenticateStaffCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var staffUser = await _staffUserRepository.GetByUsernameAsync(command.Identifier, cancellationToken);

            if (staffUser == null)
            {
                await HandlerPersistence.CommitFailureAsync(
                    _persistenceSession,
                    _auditStore,
                    command.Identifier,
                    "LOGIN",
                    "StaffUser",
                    "N/A",
                    new { command.Identifier },
                    command.CorrelationId,
                    "User not found",
                    cancellationToken);
                return CommandResult<AuthenticationResultDto>.Failure(
                    "AUTH_INVALID_CREDENTIALS",
                    command.CorrelationId);
            }

            if (!_passwordHashService.VerifyPassword(command.Password, staffUser.PasswordHash))
            {
                await HandlerPersistence.CommitFailureAsync(
                    _persistenceSession,
                    _auditStore,
                    command.Identifier,
                    "LOGIN",
                    "StaffUser",
                    staffUser.Id,
                    new { command.Identifier },
                    command.CorrelationId,
                    "Invalid password",
                    cancellationToken);
                return CommandResult<AuthenticationResultDto>.Failure(
                    "AUTH_INVALID_CREDENTIALS",
                    command.CorrelationId);
            }

            if (!staffUser.IsActive)
            {
                await HandlerPersistence.CommitFailureAsync(
                    _persistenceSession,
                    _auditStore,
                    command.Identifier,
                    "LOGIN",
                    "StaffUser",
                    staffUser.Id,
                    new { command.Identifier },
                    command.CorrelationId,
                    "User inactive",
                    cancellationToken);
                return CommandResult<AuthenticationResultDto>.Failure(
                    "User is not active",
                    command.CorrelationId);
            }

            var token = _jwtTokenService.GenerateToken(
                staffUser.Id.ToString(),
                staffUser.Username,
                staffUser.Role.ToString(),
                expiryMinutes: 60);

            var result = new AuthenticationResultDto
            {
                StaffId = staffUser.Id,
                Username = staffUser.Username,
                Email = staffUser.Email.ToString(),
                Role = staffUser.Role.ToString(),
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresInSeconds = 3600,
                AuthenticatedAt = DateTime.UtcNow,
                CorrelationId = command.CorrelationId
            };

            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                staffUser.Username,
                "LOGIN",
                "StaffUser",
                staffUser.Id,
                new { staffUser.Username, Role = staffUser.Role.ToString() },
                command.CorrelationId,
                cancellationToken);

            return CommandResult<AuthenticationResultDto>.Ok(result, command.CorrelationId, "Authentication successful");
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.Identifier,
                "LOGIN",
                "StaffUser",
                "N/A",
                new { command.Identifier },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult<AuthenticationResultDto>.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.Identifier,
                "LOGIN",
                "StaffUser",
                "N/A",
                new { command.Identifier },
                command.CorrelationId,
                ex.Message,
                cancellationToken);
            return CommandResult<AuthenticationResultDto>.Failure($"Authentication failed: {ex.Message}", command.CorrelationId);
        }
    }
}
