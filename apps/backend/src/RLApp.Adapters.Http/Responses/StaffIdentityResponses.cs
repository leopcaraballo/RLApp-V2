namespace RLApp.Adapters.Http.Responses;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresInSeconds { get; set; }
    public string Role { get; set; } = string.Empty;
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public string CorrelationId { get; set; } = string.Empty;
}

public class ChangeRoleResponse
{
    public string StaffUserId { get; set; } = string.Empty;
    public string PreviousRole { get; set; } = string.Empty;
    public string NewRole { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
