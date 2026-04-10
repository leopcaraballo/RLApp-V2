using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RLApp.Adapters.Persistence.Data.Models;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace RLApp.Adapters.Persistence.Data.Seed;

public static class StaffUserSeeder
{
    public static void Seed(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Helper for password hashing (simple PBKDF2)
        string HashPassword(string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
        }

        // Supervisor
        if (!db.StaffUsers.Any(u => u.Username == "superadmin"))
        {
            db.StaffUsers.Add(new StaffUserRecord
            {
                Id = Guid.NewGuid().ToString(),
                Username = "superadmin",
                Email = "superadmin@rlapp.local",
                PasswordHash = HashPassword("superadmin"),
                Role = "Supervisor",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        // Support
        if (!db.StaffUsers.Any(u => u.Username == "support"))
        {
            db.StaffUsers.Add(new StaffUserRecord
            {
                Id = Guid.NewGuid().ToString(),
                Username = "support",
                Email = "support@rlapp.local",
                PasswordHash = HashPassword("support"),
                Role = "Support",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        db.SaveChanges();
    }
}
