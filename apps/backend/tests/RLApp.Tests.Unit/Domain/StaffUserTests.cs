namespace RLApp.Tests.Unit.Domain;

using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.ValueObjects;

/// <summary>
/// Unit tests for the StaffUser aggregate.
/// Validates creation guards, state transitions, and role logic.
/// </summary>
public class StaffUserTests
{
    private const string CorrelationId = "corr-test";

    private static StaffUser BuildStaffUser(
        string id = "staff-1",
        string username = "jdoe",
        string email = "jdoe@clinic.com",
        string passwordHash = "hashed-pass",
        StaffRole? role = null)
        => StaffUser.Create(id, username, Email.Create(email), passwordHash, role ?? StaffRole.Doctor);

    // -------------------------------------------------------------------------
    // Create
    // -------------------------------------------------------------------------

    [Fact]
    public void Create_ValidInput_ReturnsActiveStaffUser()
    {
        var user = BuildStaffUser();

        Assert.Equal("staff-1", user.Id);
        Assert.Equal("jdoe", user.Username);
        Assert.True(user.IsActive);
        Assert.Equal(StaffRole.Doctor, user.Role);
    }

    [Fact]
    public void Create_EmptyId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            StaffUser.Create("", "jdoe", Email.Create("jdoe@clinic.com"), "hash", StaffRole.Doctor));
    }

    [Fact]
    public void Create_EmptyUsername_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            StaffUser.Create("s-1", "", Email.Create("jdoe@clinic.com"), "hash", StaffRole.Doctor));
    }

    [Fact]
    public void Create_EmptyPasswordHash_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            StaffUser.Create("s-1", "jdoe", Email.Create("jdoe@clinic.com"), "", StaffRole.Doctor));
    }

    // -------------------------------------------------------------------------
    // ChangeRole
    // -------------------------------------------------------------------------

    [Fact]
    public void ChangeRole_ToNewRole_UpdatesRole()
    {
        var user = BuildStaffUser(role: StaffRole.Doctor);
        user.ChangeRole(StaffRole.Supervisor, null, CorrelationId);

        Assert.Equal(StaffRole.Supervisor, user.Role);
        Assert.NotNull(user.UpdatedAt);
    }

    // -------------------------------------------------------------------------
    // Activate / Deactivate
    // -------------------------------------------------------------------------

    [Fact]
    public void Deactivate_ActiveUser_BecomesInactive()
    {
        var user = BuildStaffUser();
        user.Deactivate();

        Assert.False(user.IsActive);
    }

    [Fact]
    public void Deactivate_AlreadyInactiveUser_ThrowsDomainException()
    {
        var user = BuildStaffUser();
        user.Deactivate();

        Assert.Throws<DomainException>(() => user.Deactivate());
    }

    [Fact]
    public void Activate_InactiveUser_BecomesActive()
    {
        var user = BuildStaffUser();
        user.Deactivate();
        user.Activate();

        Assert.True(user.IsActive);
    }

    [Fact]
    public void Activate_AlreadyActiveUser_ThrowsDomainException()
    {
        var user = BuildStaffUser();

        Assert.Throws<DomainException>(() => user.Activate());
    }

    // -------------------------------------------------------------------------
    // HasRole
    // -------------------------------------------------------------------------

    [Fact]
    public void HasRole_CorrectRoleAndActive_ReturnsTrue()
    {
        var user = BuildStaffUser(role: StaffRole.Cashier);
        Assert.True(user.HasRole(StaffRole.Cashier));
    }

    [Fact]
    public void HasRole_WrongRole_ReturnsFalse()
    {
        var user = BuildStaffUser(role: StaffRole.Doctor);
        Assert.False(user.HasRole(StaffRole.Cashier));
    }

    [Fact]
    public void HasRole_InactiveUser_ReturnsFalse()
    {
        var user = BuildStaffUser(role: StaffRole.Doctor);
        user.Deactivate();
        Assert.False(user.HasRole(StaffRole.Doctor));
    }
}
