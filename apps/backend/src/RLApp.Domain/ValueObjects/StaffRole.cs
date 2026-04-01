namespace RLApp.Domain.ValueObjects;

/// <summary>
/// Represents a role in the system.
/// Reference: S-001 Staff Identity And Access
/// </summary>
public class StaffRole : ValueObject
{
    private static readonly HashSet<string> ValidRoles = new(StringComparer.Ordinal)
    {
        "Receptionist",
        "Cashier",
        "Doctor",
        "Supervisor",
        "Support"
    };

    public string Value { get; }

    public static readonly StaffRole Receptionist = new("Receptionist");
    public static readonly StaffRole Cashier = new("Cashier");
    public static readonly StaffRole Doctor = new("Doctor");
    public static readonly StaffRole Supervisor = new("Supervisor");
    public static readonly StaffRole Support = new("Support");

    private StaffRole(string value)
    {
        Value = value;
    }

    public static StaffRole Create(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be empty");

        if (!ValidRoles.Contains(role))
            throw new ArgumentException($"Invalid role: {role}");

        return new StaffRole(role);
    }

    public static bool IsValid(StaffRole? role)
    {
        return role is not null && ValidRoles.Contains(role.Value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
