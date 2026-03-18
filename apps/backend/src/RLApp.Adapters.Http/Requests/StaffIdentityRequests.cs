using System.ComponentModel.DataAnnotations;

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
    public string StaffUserId { get; set; } = string.Empty;

    [Required]
    public string NewRole { get; set; } = string.Empty;

    [Required]
    public string Reason { get; set; } = string.Empty;
}
