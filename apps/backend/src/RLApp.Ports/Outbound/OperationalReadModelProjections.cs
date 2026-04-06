namespace RLApp.Ports.Outbound;

public sealed class WaitingRoomMonitorProjection
{
    public string QueueId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public int WaitingCount { get; set; }
    public double AverageWaitTimeMinutes { get; set; }
    public int ActiveConsultationRooms { get; set; }
    public IReadOnlyList<OperationalStatusCountProjection> StatusBreakdown { get; set; } = Array.Empty<OperationalStatusCountProjection>();
    public IReadOnlyList<WaitingRoomMonitorEntryProjection> Entries { get; set; } = Array.Empty<WaitingRoomMonitorEntryProjection>();
}

public sealed class WaitingRoomMonitorEntryProjection
{
    public string TurnId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RoomAssigned { get; set; }
    public DateTime CheckedInAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class OperationsDashboardProjection
{
    public DateTime GeneratedAt { get; set; }
    public int CurrentWaitingCount { get; set; }
    public double AverageWaitTimeMinutes { get; set; }
    public int TotalPatientsToday { get; set; }
    public int TotalCompleted { get; set; }
    public int ActiveRooms { get; set; }
    public int ProjectionLagSeconds { get; set; }
    public IReadOnlyList<DashboardQueueSnapshotProjection> QueueSnapshots { get; set; } = Array.Empty<DashboardQueueSnapshotProjection>();
    public IReadOnlyList<OperationalStatusCountProjection> StatusBreakdown { get; set; } = Array.Empty<OperationalStatusCountProjection>();
}

public sealed class DashboardQueueSnapshotProjection
{
    public string QueueId { get; set; } = string.Empty;
    public int TotalPending { get; set; }
    public double AverageWaitTimeMinutes { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public sealed class OperationalStatusCountProjection
{
    public string Status { get; set; } = string.Empty;
    public int Total { get; set; }
}
