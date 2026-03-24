namespace RLApp.Application.DTOs;

/// <summary>
/// DTO for authentication result.
/// Implements S-001 Staff Identity And Access - Login response schema
/// </summary>
public class AuthenticationResultDto
{
    public string StaffId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresInSeconds { get; set; } = 3600;
    public DateTime AuthenticatedAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = null!;

    /// <summary>
    /// Capabilities derived from role. Can be populated from role-based authorization.
    /// </summary>
    public List<string> Capabilities { get; set; } = new();
}
