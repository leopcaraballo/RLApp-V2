namespace RLApp.Domain.Aggregates;

using Common;
using Events;

public sealed class PatientTrajectory : DomainEntity
{
    public const string ActiveState = "TrayectoriaActiva";
    public const string CompletedState = "TrayectoriaFinalizada";
    public const string CancelledState = "TrayectoriaCancelada";

    public const string ReceptionStage = "Recepcion";
    public const string CashierStage = "Caja";
    public const string ConsultationStage = "Consulta";

    /// <summary>
    /// Allowed stage transitions. Key = current stage, Values = valid next stages.
    /// Empty string represents the initial state (no stage recorded yet).
    /// RN-09 / RN-10: transitions must respect the allowed flow.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new(StringComparer.Ordinal)
    {
        [string.Empty] = new(StringComparer.Ordinal) { ReceptionStage },
        [ReceptionStage] = new(StringComparer.Ordinal) { CashierStage },
        [CashierStage] = new(StringComparer.Ordinal) { ConsultationStage },
        [ConsultationStage] = new(StringComparer.Ordinal) { ConsultationStage }  // allow re-recording final stage on completion
    };

    public string? CurrentStage => Stages.Count > 0 ? Stages[^1].Stage : null;

    public string PatientId { get; private set; }
    public string QueueId { get; private set; }
    public string CurrentState { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public List<TrajectoryStage> Stages { get; } = new();
    public List<string> CorrelationIds { get; } = new();

    private PatientTrajectory(string trajectoryId, string patientId, string queueId)
        : base(trajectoryId)
    {
        PatientId = patientId;
        QueueId = queueId;
        CurrentState = ActiveState;
        OpenedAt = DateTime.UtcNow;
    }

    public static PatientTrajectory Start(
        string trajectoryId,
        string patientId,
        string queueId,
        string stage,
        string sourceEvent,
        string? sourceState,
        DateTime occurredAt,
        string correlationId)
    {
        ValidateIdentifiers(trajectoryId, patientId, queueId, correlationId);

        var trajectory = new PatientTrajectory(trajectoryId, patientId, queueId)
        {
            OpenedAt = occurredAt,
            CurrentState = ActiveState
        };

        trajectory.AddCorrelationId(correlationId);
        trajectory.RaiseDomainEvent(new PatientTrajectoryOpened(trajectoryId, patientId, queueId, occurredAt, correlationId));
        trajectory.RecordStageInternal(stage, sourceEvent, sourceState, occurredAt, correlationId, raiseDomainEvent: true);
        return trajectory;
    }

    public bool RecordStage(string stage, string sourceEvent, string? sourceState, DateTime occurredAt, string correlationId)
    {
        EnsureActiveForMutation();

        if (HasDuplicateStage(stage, sourceEvent, sourceState, occurredAt, correlationId))
        {
            AddCorrelationId(correlationId);
            return false;
        }

        EnsureValidTransition(stage);
        EnsureChronologicalOrder(occurredAt);
        RecordStageInternal(stage, sourceEvent, sourceState, occurredAt, correlationId, raiseDomainEvent: true);
        return true;
    }

    public bool Complete(string stage, string sourceEvent, string? sourceState, DateTime occurredAt, string correlationId)
    {
        if (CurrentState == CompletedState)
        {
            AddCorrelationId(correlationId);
            return false;
        }

        if (CurrentState == CancelledState)
            throw new DomainException("A cancelled trajectory cannot be completed");

        EnsureChronologicalOrder(occurredAt);
        AddCorrelationId(correlationId);

        if (!HasDuplicateStage(stage, sourceEvent, sourceState, occurredAt, correlationId))
        {
            RecordStageInternal(stage, sourceEvent, sourceState, occurredAt, correlationId, raiseDomainEvent: false);
        }

        CurrentState = CompletedState;
        ClosedAt = occurredAt;
        RaiseDomainEvent(new PatientTrajectoryCompleted(Id, PatientId, QueueId, stage, sourceEvent, sourceState, occurredAt, correlationId));
        return true;
    }

    public bool Cancel(string sourceEvent, string? sourceState, string? reason, DateTime occurredAt, string correlationId)
    {
        if (CurrentState == CancelledState)
        {
            AddCorrelationId(correlationId);
            return false;
        }

        if (CurrentState == CompletedState)
            throw new DomainException("A completed trajectory cannot be cancelled");

        EnsureChronologicalOrder(occurredAt);
        AddCorrelationId(correlationId);
        CurrentState = CancelledState;
        ClosedAt = occurredAt;
        RaiseDomainEvent(new PatientTrajectoryCancelled(Id, PatientId, QueueId, sourceEvent, sourceState, reason, occurredAt, correlationId));
        return true;
    }

    public bool RecordRebuild(string scope, DateTime occurredAt, string correlationId)
    {
        if (CorrelationIds.Contains(correlationId, StringComparer.Ordinal))
        {
            return false;
        }

        EnsureChronologicalOrder(occurredAt);
        AddCorrelationId(correlationId);
        RaiseDomainEvent(new PatientTrajectoryRebuilt(Id, PatientId, QueueId, scope, occurredAt, correlationId));
        return true;
    }

    private static void ValidateIdentifiers(string trajectoryId, string patientId, string queueId, string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trajectoryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(patientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
    }

    private void EnsureActiveForMutation()
    {
        if (CurrentState == CompletedState || CurrentState == CancelledState)
            throw new DomainException("A closed trajectory does not accept new stages");
    }

    private void EnsureValidTransition(string targetStage)
    {
        var current = CurrentStage ?? string.Empty;
        if (AllowedTransitions.TryGetValue(current, out var allowed) && allowed.Contains(targetStage))
            return;

        throw new DomainException($"Invalid stage transition from '{(CurrentStage ?? "(none)")}' to '{targetStage}'");
    }

    private void EnsureChronologicalOrder(DateTime occurredAt)
    {
        var lastStage = Stages.LastOrDefault();
        if (lastStage is not null && occurredAt < lastStage.OccurredAt)
            throw new DomainException("Trajectory stages must be recorded in chronological order");

        if (ClosedAt.HasValue && occurredAt < ClosedAt.Value)
            throw new DomainException("Trajectory cannot record data before its closing time");
    }

    private bool HasDuplicateStage(string stage, string sourceEvent, string? sourceState, DateTime occurredAt, string correlationId) =>
        Stages.Any(existing =>
            existing.Stage == stage &&
            existing.SourceEvent == sourceEvent &&
            existing.SourceState == sourceState &&
            existing.OccurredAt == occurredAt &&
            existing.CorrelationId == correlationId);

    private void AddCorrelationId(string correlationId)
    {
        if (!CorrelationIds.Contains(correlationId, StringComparer.Ordinal))
        {
            CorrelationIds.Add(correlationId);
        }
    }

    private void RecordStageInternal(
        string stage,
        string sourceEvent,
        string? sourceState,
        DateTime occurredAt,
        string correlationId,
        bool raiseDomainEvent)
    {
        if (string.IsNullOrWhiteSpace(stage))
            throw new DomainException("Trajectory stage cannot be empty");

        if (string.IsNullOrWhiteSpace(sourceEvent))
            throw new DomainException("Source event cannot be empty");

        Stages.Add(new TrajectoryStage
        {
            OccurredAt = occurredAt,
            Stage = stage,
            SourceEvent = sourceEvent,
            SourceState = sourceState,
            CorrelationId = correlationId
        });

        AddCorrelationId(correlationId);

        if (raiseDomainEvent)
        {
            RaiseDomainEvent(new PatientTrajectoryStageRecorded(
                Id,
                PatientId,
                QueueId,
                stage,
                sourceEvent,
                sourceState,
                occurredAt,
                correlationId));
        }
    }
}

public sealed class TrajectoryStage
{
    public DateTime OccurredAt { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string SourceEvent { get; set; } = string.Empty;
    public string? SourceState { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
