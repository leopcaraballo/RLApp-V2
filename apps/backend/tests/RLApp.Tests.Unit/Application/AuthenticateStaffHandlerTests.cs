namespace RLApp.Tests.Unit.Application;

using NSubstitute;
using RLApp.Application.Commands;
using RLApp.Application.Handlers;
using RLApp.Domain.Aggregates;
using RLApp.Domain.ValueObjects;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

/// <summary>
/// Unit tests for AuthenticateStaffHandler.
/// Validates authentication flows: success, user not found, wrong password, inactive user.
/// </summary>
public class AuthenticateStaffHandlerTests
{
    // Shared mocks
    private readonly IStaffUserRepository _staffUserRepo = Substitute.For<IStaffUserRepository>();
    private readonly IPasswordHashService _passwordHashService = Substitute.For<IPasswordHashService>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly IAuditStore _auditStore = Substitute.For<IAuditStore>();
    private readonly IPersistenceSession _persistenceSession = Substitute.For<IPersistenceSession>();

    private AuthenticateStaffHandler BuildHandler() =>
        new(_staffUserRepo, _passwordHashService, _jwtTokenService, _auditStore, _persistenceSession);

    public AuthenticateStaffHandlerTests()
    {
        _auditStore.RecordAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _persistenceSession.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    private static StaffUser MakeActiveStaff(string id = "s-1", string username = "jdoe", bool isActive = true)
    {
        var user = StaffUser.Create(id, username, Email.Create("jdoe@clinic.com"), "hashed-pass", StaffRole.Doctor);
        if (!isActive) user.Deactivate();
        return user;
    }

    private static AuthenticateStaffCommand MakeCommand(string username = "jdoe", string password = "secret123") =>
        new(username, password, correlationId: "corr-1");

    // -------------------------------------------------------------------------
    // Success path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsOkWithToken()
    {
        var staff = MakeActiveStaff();
        _staffUserRepo.GetByUsernameAsync("jdoe").Returns((StaffUser?)staff);
        _passwordHashService.VerifyPassword("secret123", "hashed-pass").Returns(true);
        _jwtTokenService.GenerateToken(staff.Id.ToString(), staff.Username, staff.Role.ToString(), Arg.Any<int>())
            .Returns("jwt-token-abc");

        var result = await BuildHandler().Handle(MakeCommand(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("jwt-token-abc", result.Data!.AccessToken);
        Assert.Equal("Bearer", result.Data.TokenType);
        Assert.Equal("jdoe", result.Data.Username);
    }

    // -------------------------------------------------------------------------
    // Failure paths
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        _staffUserRepo.GetByUsernameAsync("jdoe").Returns((StaffUser?)null);

        var result = await BuildHandler().Handle(MakeCommand(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("AUTH_INVALID_CREDENTIALS", result.Message);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailure()
    {
        var staff = MakeActiveStaff();
        _staffUserRepo.GetByUsernameAsync("jdoe").Returns(staff);
        _passwordHashService.VerifyPassword("secret123", "hashed-pass").Returns(false);

        var result = await BuildHandler().Handle(MakeCommand(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("AUTH_INVALID_CREDENTIALS", result.Message);
    }

    [Fact]
    public async Task Handle_InactiveUser_ReturnsFailure()
    {
        var staff = MakeActiveStaff(isActive: false);
        _staffUserRepo.GetByUsernameAsync("jdoe").Returns(staff);
        _passwordHashService.VerifyPassword("secret123", "hashed-pass").Returns(true);

        var result = await BuildHandler().Handle(MakeCommand(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.Message);
    }
}
