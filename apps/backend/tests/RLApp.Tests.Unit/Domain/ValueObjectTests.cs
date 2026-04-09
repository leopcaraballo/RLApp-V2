namespace RLApp.Tests.Unit.Domain;

using RLApp.Domain.ValueObjects;

public class ValueObjectTests
{
    // ── Email ─────────────────────────────────────────────────────

    [Fact]
    public void Email_Create_ValidEmail_ReturnsLowercaseEmail()
    {
        var email = Email.Create("User@Example.COM");
        Assert.Equal("user@example.com", email.Value);
    }

    [Fact]
    public void Email_Create_NullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Email.Create(""));
        Assert.Throws<ArgumentException>(() => Email.Create("   "));
    }

    [Fact]
    public void Email_Create_NoAtSymbol_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Email.Create("invalid-email"));
    }

    [Fact]
    public void Email_Equality_SameValue_AreEqual()
    {
        var a = Email.Create("test@example.com");
        var b = Email.Create("TEST@EXAMPLE.COM");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Email_Equality_DifferentValue_AreNotEqual()
    {
        var a = Email.Create("a@example.com");
        var b = Email.Create("b@example.com");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Email_ToString_ReturnsValue()
    {
        var email = Email.Create("test@example.com");
        Assert.Equal("test@example.com", email.ToString());
    }

    // ── StaffRole ─────────────────────────────────────────────────

    [Theory]
    [InlineData("Receptionist")]
    [InlineData("Cashier")]
    [InlineData("Doctor")]
    [InlineData("Supervisor")]
    [InlineData("Support")]
    public void StaffRole_Create_ValidRole_ReturnsStaffRole(string roleName)
    {
        var role = StaffRole.Create(roleName);
        Assert.Equal(roleName, role.Value);
    }

    [Fact]
    public void StaffRole_Create_EmptyRole_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => StaffRole.Create(""));
        Assert.Throws<ArgumentException>(() => StaffRole.Create("   "));
    }

    [Fact]
    public void StaffRole_Create_InvalidRole_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => StaffRole.Create("Admin"));
        Assert.Throws<ArgumentException>(() => StaffRole.Create("RECEPTIONIST")); // Case-sensitive
    }

    [Fact]
    public void StaffRole_IsValid_NullRole_ReturnsFalse()
    {
        Assert.False(StaffRole.IsValid(null));
    }

    [Fact]
    public void StaffRole_IsValid_ValidRole_ReturnsTrue()
    {
        Assert.True(StaffRole.IsValid(StaffRole.Receptionist));
        Assert.True(StaffRole.IsValid(StaffRole.Doctor));
    }

    [Fact]
    public void StaffRole_Equality_SameValue_AreEqual()
    {
        var a = StaffRole.Create("Receptionist");
        var b = StaffRole.Receptionist;
        Assert.Equal(a, b);
    }

    [Fact]
    public void StaffRole_StaticInstances_HaveCorrectValues()
    {
        Assert.Equal("Receptionist", StaffRole.Receptionist.Value);
        Assert.Equal("Cashier", StaffRole.Cashier.Value);
        Assert.Equal("Doctor", StaffRole.Doctor.Value);
        Assert.Equal("Supervisor", StaffRole.Supervisor.Value);
        Assert.Equal("Support", StaffRole.Support.Value);
    }
}
