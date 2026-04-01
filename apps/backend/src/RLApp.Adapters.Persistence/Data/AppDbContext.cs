using Microsoft.EntityFrameworkCore;
using RLApp.Adapters.Persistence.Data.Models;

namespace RLApp.Adapters.Persistence.Data;

public class AppDbContext : DbContext
{
    public DbSet<EventRecord> EventStore { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<OutboxDeadLetterMessage> OutboxDeadLetterMessages { get; set; } = null!;
    public DbSet<StaffUserRecord> StaffUsers { get; set; } = null!;
    public DbSet<AuditLogRecord> AuditLogs { get; set; } = null!;

    // Read Models
    public DbSet<WaitingRoomMonitorView> WaitingRoomMonitors { get; set; } = null!;
    public DbSet<QueueStateView> QueueStates { get; set; } = null!;
    public DbSet<NextTurnView> NextTurns { get; set; } = null!;
    public DbSet<RecentHistoryView> RecentHistories { get; set; } = null!;
    public DbSet<OperationsDashboardView> OperationsDashboards { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from assembly
        modelBuilder.ApplyConfiguration(new EventRecord.Configuration());
        modelBuilder.ApplyConfiguration(new OutboxMessage.Configuration());
        modelBuilder.ApplyConfiguration(new OutboxDeadLetterMessage.Configuration());
        modelBuilder.ApplyConfiguration(new StaffUserRecord.Configuration());
        modelBuilder.ApplyConfiguration(new AuditLogRecord.Configuration());

        // Read Models configuration
        ReadModelsConfiguration.Configure(modelBuilder);
    }
}
