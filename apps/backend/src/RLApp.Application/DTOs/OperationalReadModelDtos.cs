namespace RLApp.Application.DTOs;

public sealed class WaitingRoomMonitorDto
{
    public string QueueId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public int WaitingCount { get; set; }
    public double AverageWaitTimeMinutes { get; set; }
    public int ActiveConsultationRooms { get; set; }
    public IReadOnlyList<OperationalStatusCountDto> StatusBreakdown { get; set; } = Array.Empty<OperationalStatusCountDto>();
    public IReadOnlyList<WaitingRoomMonitorEntryDto> Entries { get; set; } = Array.Empty<WaitingRoomMonitorEntryDto>();
}

public sealed class WaitingRoomMonitorEntryDto
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

public sealed class OperationsDashboardSnapshotDto
{
    public DateTime GeneratedAt { get; set; }
    public int CurrentWaitingCount { get; set; }
    public double AverageWaitTimeMinutes { get; set; }
    public int TotalPatientsToday { get; set; }
    public int TotalCompleted { get; set; }
    public int ActiveRooms { get; set; }
    public int ProjectionLagSeconds { get; set; }
    public IReadOnlyList<DashboardQueueSnapshotDto> QueueSnapshots { get; set; } = Array.Empty<DashboardQueueSnapshotDto>();
    public IReadOnlyList<OperationalStatusCountDto> StatusBreakdown { get; set; } = Array.Empty<OperationalStatusCountDto>();
}

public sealed class DashboardQueueSnapshotDto
{
    public string QueueId { get; set; } = string.Empty;
    public int TotalPending { get; set; }
    public double AverageWaitTimeMinutes { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public sealed class OperationalStatusCountDto
{
    public string Status { get; set; } = string.Empty;
    public int Total { get; set; }
}
