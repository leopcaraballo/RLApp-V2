using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RLApp.Adapters.Http.Requests;

public class LoginRequest
{
    [Required]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class ChangeRoleRequest
{
    [Required]
    [JsonPropertyName("staffUserId")]
    public string StaffUserId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("newRole")]
    public string NewRole { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}
