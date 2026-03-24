namespace RLApp.Domain.Aggregates;

using Common;
using ValueObjects;
using Events;

/// <summary>
/// StaffUser Aggregate Root
/// Responsible for staff credentials, roles and access state.
/// Reference: Aggregates.md, S-001 Staff Identity And Access
/// </summary>
public class StaffUser : DomainEntity
{
    public string Username { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public StaffRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private StaffUser(string id, string username, Email email, string passwordHash, StaffRole role)
        : base(id)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create a new staff user.
    /// </summary>
    public static StaffUser Create(string id, string username, Email email, string passwordHash, StaffRole role)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("Staff user ID cannot be empty");
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Username cannot be empty");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash cannot be empty");

        return new StaffUser(id, username, email, passwordHash, role);
    }

    /// <summary>
    /// Change the role of the staff user.
    /// </summary>
    public void ChangeRole(StaffRole newRole, string? reason, string correlationId)
    {
        // Enums are value types and cannot be null
        if (!Enum.IsDefined(typeof(StaffRole), newRole))
            throw new DomainException("Invalid staff role");

        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new StaffRoleChanged(Id, Id, newRole.ToString(), reason, correlationId));
    }

    /// <summary>
    /// Deactivate the staff user.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("Staff user is already inactive");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activate the staff user.
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            throw new DomainException("Staff user is already active");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Verify if the staff user has the required role.
    /// </summary>
    public bool HasRole(StaffRole role)
    {
        return Role == role && IsActive;
    }
}
