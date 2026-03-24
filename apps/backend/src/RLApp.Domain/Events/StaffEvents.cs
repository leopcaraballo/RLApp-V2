namespace RLApp.Domain.Events;

using Common;
using System.Text.Json.Serialization;
using RLApp.Domain.ValueObjects;

/// <summary>
/// EV-015 StaffRoleChanged
/// Raised when a staff member's role is updated.
/// Reference: S-001 Staff Identity And Access
/// </summary>
public class StaffRoleChanged : DomainEvent
{
    [JsonPropertyName("staffId")]
    public string StaffId { get; set; } = string.Empty;

    [JsonPropertyName("newRole")]
    public string NewRole { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    public StaffRoleChanged(string aggregateId, string staffId, string newRole, string? reason, string correlationId)
        : base(nameof(StaffRoleChanged), aggregateId, correlationId)
    {
        StaffId = staffId;
        NewRole = newRole;
        Reason = reason;
    }

    protected StaffRoleChanged() { }
}
