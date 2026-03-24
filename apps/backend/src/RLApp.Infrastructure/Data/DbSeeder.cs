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
    /// WARNING: Change these credentials immediately in production!
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seed operation...");

            // Always ensure default superadmin has the correct password hash
            await SeedDefaultSuperadmin();
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
    {
        const string defaultPassword = "SuperAdmin@2026Dev!";
        const string devUsername = "superadmin";
        const string superadminId = "staff-superadmin";

        var hashedPassword = _passwordHashService.HashPassword(defaultPassword);

        // Check if superadmin exists
        var existingAdmin = await _context.StaffUsers
            .FirstOrDefaultAsync(s => s.Id == superadminId);

        if (existingAdmin != null)
        {
            // Update existing admin's credentials
            existingAdmin.PasswordHash = hashedPassword;
            existingAdmin.IsActive = true;
            existingAdmin.UpdatedAt = DateTime.UtcNow;
            _context.StaffUsers.Update(existingAdmin);
            _logger.LogInformation("Updated existing superadmin user with new credentials");
            return;
        }

        // Create new superadmin
        var superadmin = new StaffUserRecord
        {
            Id = superadminId,
            Username = devUsername,
            Email = "superadmin@rlapp.local",
            PasswordHash = hashedPassword,
            Role = "Supervisor", // Supervisor role has full access per authorization policies
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        _context.StaffUsers.Add(superadmin);

        _logger.LogInformation(
            "Seeded default superadmin user. Username: {Username}, Email: {Email}, Role: {Role}",
            devUsername,
            superadmin.Email,
            superadmin.Role);
    }
}
