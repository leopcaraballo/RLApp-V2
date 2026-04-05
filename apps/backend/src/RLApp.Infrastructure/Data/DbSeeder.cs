using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Ports.Outbound;

namespace RLApp.Infrastructure.Data;

/// <summary>
/// Database seeding service for initial data population.
/// Used to create default development users and bootstrap initial state.
/// Reference: S-001 Staff Identity And Access
/// </summary>
public class DbSeeder
{
    private readonly AppDbContext _context;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(AppDbContext context, IPasswordHashService passwordHashService, ILogger<DbSeeder> logger)
    {
        _context = context;
        _passwordHashService = passwordHashService;
        _logger = logger;
    }

    /// <summary>
    /// Seeds initial development data into the database.
    /// This is called automatically during application startup if configured.
    ///
    /// Default Superadmin Credentials (Development Only):
    /// - Username: superadmin
    /// - Password: SuperAdmin@2026Dev!
    /// - Role: Supervisor (all permissions)
    ///
    /// Default Support Credentials (Development Only):
    /// - Username: support
    /// - Password: Support@2026Dev!
    /// - Role: Support (controlled rebuild and diagnostic workflows)
    ///
    /// WARNING: Change these credentials immediately in production!
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seed operation...");

            // Always ensure default superadmin has the correct password hash
            await SeedDefaultSuperadmin();
            await SeedDefaultSupport();
            await _context.SaveChangesAsync();

            _logger.LogInformation("Database seed operation completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seed operation");
            throw;
        }
    }

    /// <summary>
    /// Creates or updates the default superadmin user for development/initial setup.
    /// Always refreshes the password hash to ensure it matches the development credentials.
    /// </summary>
    private async Task SeedDefaultSuperadmin()
        => await SeedDefaultUser(
            superadminId: "staff-superadmin",
            username: "superadmin",
            email: "superadmin@rlapp.local",
            password: "SuperAdmin@2026Dev!",
            role: "Supervisor",
            logLabel: "superadmin");

    /// <summary>
    /// Creates or updates the default support user for rebuild and diagnostics workflows.
    /// Always refreshes the password hash to ensure it matches the development credentials.
    /// </summary>
    private async Task SeedDefaultSupport()
        => await SeedDefaultUser(
            superadminId: "staff-support-01",
            username: "support",
            email: "support@rlapp.local",
            password: "Support@2026Dev!",
            role: "Support",
            logLabel: "support");

    private async Task SeedDefaultUser(
        string superadminId,
        string username,
        string email,
        string password,
        string role,
        string logLabel)
    {
        var hashedPassword = _passwordHashService.HashPassword(password);

        // Check if the default user exists
        var existingAdmin = await _context.StaffUsers
            .FirstOrDefaultAsync(s => s.Id == superadminId);

        if (existingAdmin != null)
        {
            // Update existing default user's credentials
            existingAdmin.PasswordHash = hashedPassword;
            existingAdmin.IsActive = true;
            existingAdmin.Username = username;
            existingAdmin.Email = email;
            existingAdmin.Role = role;
            existingAdmin.UpdatedAt = DateTime.UtcNow;
            _context.StaffUsers.Update(existingAdmin);
            _logger.LogInformation("Updated existing default {LogLabel} user with new credentials", logLabel);
            return;
        }

        // Create new default user
        var superadmin = new StaffUserRecord
        {
            Id = superadminId,
            Username = username,
            Email = email,
            PasswordHash = hashedPassword,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        _context.StaffUsers.Add(superadmin);

        _logger.LogInformation(
            "Seeded default {LogLabel} user. Username: {Username}, Email: {Email}, Role: {Role}",
            logLabel,
            username,
            superadmin.Email,
            superadmin.Role);
    }
}
