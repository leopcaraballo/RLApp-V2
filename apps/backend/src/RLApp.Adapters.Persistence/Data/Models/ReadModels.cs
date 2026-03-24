using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RLApp.Adapters.Persistence.Data.Models;

[Table("v_waiting_room_monitor")]
public class WaitingRoomMonitorView
{
    [Key]
    public string TurnId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RoomAssigned { get; set; }
    public DateTime UpdatedAt { get; set; }
}

[Table("v_queue_state")]
public class QueueStateView
{
    [Key]
    public string QueueId { get; set; } = string.Empty;
    public int TotalPending { get; set; }
    public double AverageWaitTimeMinutes { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

[Table("v_next_turn")]
public class NextTurnView
{
    [Key]
    public string QueueId { get; set; } = string.Empty;
    public string TurnId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
}

[Table("v_recent_history")]
public class RecentHistoryView
{
    [Key]
    public string TurnId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Outcome { get; set; } = string.Empty;
}

[Table("v_operations_dashboard")]
public class OperationsDashboardView
{
    [Key]
    public string Id { get; set; } = "SYSTEM_SINGLETON";
    public int TotalPatientsToday { get; set; }
    public int ActiveRooms { get; set; }
    public int TotalCompleted { get; set; }
    public DateTime Date { get; set; }
}

public class ReadModelsConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WaitingRoomMonitorView>().ToTable("v_waiting_room_monitor");
        modelBuilder.Entity<QueueStateView>().ToTable("v_queue_state");
        modelBuilder.Entity<NextTurnView>().ToTable("v_next_turn");
        modelBuilder.Entity<RecentHistoryView>().ToTable("v_recent_history");
        modelBuilder.Entity<OperationsDashboardView>().ToTable("v_operations_dashboard");
    }
}
