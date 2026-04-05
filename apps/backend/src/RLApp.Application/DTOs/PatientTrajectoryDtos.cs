namespace RLApp.Application.DTOs;

public sealed class PatientTrajectoryDto
{
    public string TrajectoryId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string QueueId { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public IReadOnlyList<string> CorrelationIds { get; set; } = Array.Empty<string>();
    public IReadOnlyList<PatientTrajectoryStageDto> Stages { get; set; } = Array.Empty<PatientTrajectoryStageDto>();
}

public sealed class PatientTrajectoryStageDto
{
    public DateTime OccurredAt { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string SourceEvent { get; set; } = string.Empty;
    public string? SourceState { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class RebuildPatientTrajectoriesResultDto
{
    public string JobId { get; set; } = string.Empty;
    public DateTime AcceptedAt { get; set; }
    public string Scope { get; set; } = string.Empty;
    public bool DryRun { get; set; }
    public string Status { get; set; } = string.Empty;
    public int EventsProcessed { get; set; }
    public int TrajectoriesProcessed { get; set; }
}
