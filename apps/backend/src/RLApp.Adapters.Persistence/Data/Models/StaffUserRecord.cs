using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RLApp.Adapters.Persistence.Data.Models;

public class StaffUserRecord
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public class Configuration : IEntityTypeConfiguration<StaffUserRecord>
    {
        public void Configure(EntityTypeBuilder<StaffUserRecord> builder)
        {
            builder.ToTable("staff_users");
            builder.HasKey(e => e.Id);
            builder.HasIndex(e => e.Username).IsUnique();
            builder.HasIndex(e => e.Email).IsUnique();

            builder.Property(e => e.Username).IsRequired().HasMaxLength(100);
            builder.Property(e => e.Email).IsRequired().HasMaxLength(255);
            builder.Property(e => e.PasswordHash).IsRequired();
            builder.Property(e => e.Role).IsRequired().HasMaxLength(50);
        }
    }
}
