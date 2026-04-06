namespace RLApp.Ports.Outbound;

public sealed class PatientTrajectoryProjection
{
    public string TrajectoryId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string QueueId { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public IReadOnlyList<string> CorrelationIds { get; set; } = Array.Empty<string>();
    public IReadOnlyList<PatientTrajectoryStageProjection> Stages { get; set; } = Array.Empty<PatientTrajectoryStageProjection>();
}

public sealed class PatientTrajectoryStageProjection
{
    public DateTime OccurredAt { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string SourceEvent { get; set; } = string.Empty;
    public string? SourceState { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
