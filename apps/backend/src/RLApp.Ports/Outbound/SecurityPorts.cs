namespace RLApp.Ports.Outbound;

using System.Security.Claims;

/// <summary>
/// Outbound port for JWT token generation and validation.
/// Implements S-001: Staff Identity And Access
/// Pattern: Adapter for security/authentication services
/// </summary>
public interface IJwtTokenService
{
    string GenerateToken(string staffId, string username, string role, int expiryMinutes = 60);
    ClaimsPrincipal? ValidateToken(string token);
}

/// <summary>
/// Outbound port for password hashing and verification.
/// Implements S-001: Staff Identity And Access
/// Pattern: Adapter for security/cryptography services
/// </summary>
public interface IPasswordHashService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
