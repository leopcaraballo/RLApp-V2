namespace RLApp.Application.Handlers;

using Commands;
using DTOs;
using RLApp.Application.DTOs;
using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

/// <summary>
/// Handler for UC-001: Authenticate Staff
/// Validates credentials and returns authentication token metadata.
/// Reference: S-001 Staff Identity And Access
/// </summary>
public class AuthenticateStaffHandler
{
    private readonly IStaffUserRepository _staffUserRepository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthenticateStaffHandler(
        IStaffUserRepository staffUserRepository,
        IPasswordHashService passwordHashService,
        IJwtTokenService jwtTokenService)
    {
        _staffUserRepository = staffUserRepository;
        _passwordHashService = passwordHashService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<CommandResult<AuthenticationResultDto>> Handle(AuthenticateStaffCommand command)
    {
        try
        {
            var staffUser = await _staffUserRepository.GetByUsernameAsync(command.Username);

            if (staffUser == null)
                return CommandResult<AuthenticationResultDto>.Failure(
                    "AUTH_INVALID_CREDENTIALS",
                    command.CorrelationId);

            if (!_passwordHashService.VerifyPassword(command.Password, staffUser.PasswordHash))
                return CommandResult<AuthenticationResultDto>.Failure(
                    "AUTH_INVALID_CREDENTIALS",
                    command.CorrelationId);

            if (!staffUser.IsActive)
                return CommandResult<AuthenticationResultDto>.Failure(
                    "User is not active",
                    command.CorrelationId);

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

            return CommandResult<AuthenticationResultDto>.Ok(result, command.CorrelationId, "Authentication successful");
        }
        catch (DomainException ex)
        {
            return CommandResult<AuthenticationResultDto>.Failure(ex.Message, command.CorrelationId);
        }
        catch (Exception ex)
        {
            return CommandResult<AuthenticationResultDto>.Failure($"Authentication failed: {ex.Message}", command.CorrelationId);
        }
    }
}
