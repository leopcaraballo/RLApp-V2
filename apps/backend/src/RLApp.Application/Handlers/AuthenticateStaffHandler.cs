namespace RLApp.Application.Handlers;

using Commands;
using DTOs;
using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Ports.Inbound;

/// <summary>
/// Handler for UC-001: Authenticate Staff
/// Validates credentials and returns authentication token metadata.
/// Reference: S-001 Staff Identity And Access
/// </summary>
public class AuthenticateStaffHandler
{
    private readonly IStaffUserRepository _staffUserRepository;

    public AuthenticateStaffHandler(IStaffUserRepository staffUserRepository)
    {
        _staffUserRepository = staffUserRepository;
    }

    public async Task<CommandResult<AuthenticationResultDto>> Handle(AuthenticateStaffCommand command)
    {
        try
        {
            var staffUser = await _staffUserRepository.GetByUsernameAsync(command.Username);

            if (staffUser == null)
                return CommandResult<AuthenticationResultDto>.Failure("Invalid credentials", command.CorrelationId);

            // TODO: Password verification would happen here with proper hashing algorithm
            if (!staffUser.IsActive)
                return CommandResult<AuthenticationResultDto>.Failure("User is not active", command.CorrelationId);

            var result = new AuthenticationResultDto
            {
                StaffId = staffUser.Id,
                Username = staffUser.Username,
                Email = staffUser.Email.ToString(),
                Role = staffUser.Role.ToString(),
                AuthenticatedAt = DateTime.UtcNow
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

/// <summary>
/// DTO for authentication result.
/// </summary>
public class AuthenticationResultDto
{
    public string StaffId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public DateTime AuthenticatedAt { get; set; }
}
