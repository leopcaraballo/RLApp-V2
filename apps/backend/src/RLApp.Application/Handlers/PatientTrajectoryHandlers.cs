namespace RLApp.Application.Handlers;

using System.Collections.Concurrent;
using System.Diagnostics;
using Commands;
using DTOs;
using MediatR;
using Microsoft.Extensions.Logging;
using Observability;
using Queries;
using RLApp.Application.Services;
using RLApp.Domain.Aggregates;
using RLApp.Domain.Common;
using RLApp.Domain.Events;
using RLApp.Ports.Inbound;
using RLApp.Ports.Outbound;

public sealed class DiscoverPatientTrajectoriesHandler : IRequestHandler<DiscoverPatientTrajectoriesQuery, QueryResult<PatientTrajectoryDiscoveryResponseDto>>
{
    private readonly IProjectionStore _projectionStore;
    private readonly ILogger<DiscoverPatientTrajectoriesHandler> _logger;

    public DiscoverPatientTrajectoriesHandler(
        IProjectionStore projectionStore,
        ILogger<DiscoverPatientTrajectoriesHandler> logger)
    {
        _projectionStore = projectionStore;
        _logger = logger;
    }

    public async Task<QueryResult<PatientTrajectoryDiscoveryResponseDto>> Handle(
        DiscoverPatientTrajectoriesQuery query,
        CancellationToken cancellationToken)
    {
        var patientId = query.PatientId.Trim();
        var queueId = string.IsNullOrWhiteSpace(query.QueueId) ? null : query.QueueId.Trim();
        var queueFilterApplied = !string.IsNullOrWhiteSpace(queueId);
        using var activity = PatientTrajectoryTelemetry.StartDiscoveryActivity(query.CorrelationId, patientId, queueId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(patientId))
            {
                _logger.LogWarning(
                    "Trajectory discovery rejected due to invalid scope. CorrelationId: {CorrelationId}, QueueId: {QueueId}",
                    query.CorrelationId,
                    queueId);

                stopwatch.Stop();
                PatientTrajectoryTelemetry.RecordDiscoveryRejected(stopwatch.Elapsed, queueFilterApplied, "invalid_scope");
                PatientTrajectoryTelemetry.SetDiscoveryResult(activity, "invalid_scope", 0);

                return QueryResult<PatientTrajectoryDiscoveryResponseDto>.Failure(
                    "TRAJECTORY_DISCOVERY_SCOPE_INVALID",
                    query.CorrelationId);
            }

            var projections = await _projectionStore.FindPatientTrajectoriesAsync(patientId, queueId, cancellationToken);
            var items = projections
                .Select(projection => new PatientTrajectoryDiscoveryItemDto
                {
                    TrajectoryId = projection.TrajectoryId,
                    PatientId = projection.PatientId,
                    QueueId = projection.QueueId,
                    CurrentState = projection.CurrentState,
                    OpenedAt = projection.OpenedAt,
                    ClosedAt = projection.ClosedAt,
                    LastCorrelationId = projection.CorrelationIds.LastOrDefault()
                })
                .ToArray();

            _logger.LogInformation(
                "Trajectory discovery completed. CorrelationId: {CorrelationId}, PatientId: {PatientId}, QueueId: {QueueId}, MatchCount: {MatchCount}",
                query.CorrelationId,
                patientId,
                queueId,
                items.Length);

            stopwatch.Stop();
            PatientTrajectoryTelemetry.RecordDiscoveryCompleted(items.Length, stopwatch.Elapsed, queueFilterApplied);
            PatientTrajectoryTelemetry.SetDiscoveryResult(activity, "success", items.Length);

            return QueryResult<PatientTrajectoryDiscoveryResponseDto>.Ok(
                new PatientTrajectoryDiscoveryResponseDto
                {
                    Total = items.Length,
                    Items = items
                },
                query.CorrelationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            PatientTrajectoryTelemetry.RecordDiscoveryFailed(stopwatch.Elapsed, queueFilterApplied, ex.GetType().Name);
            PatientTrajectoryTelemetry.RecordFailure(activity, ex);
            throw;
        }
    }
}

public sealed class GetPatientTrajectoryHandler : IRequestHandler<GetPatientTrajectoryQuery, QueryResult<PatientTrajectoryDto>>
{
    private readonly IProjectionStore _projectionStore;

    public GetPatientTrajectoryHandler(IProjectionStore projectionStore)
    {
        _projectionStore = projectionStore;
    }

    public async Task<QueryResult<PatientTrajectoryDto>> Handle(GetPatientTrajectoryQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.TrajectoryId))
        {
            return QueryResult<PatientTrajectoryDto>.Failure("TRAJECTORY_NOT_FOUND", query.CorrelationId);
        }

        var projection = await _projectionStore.GetPatientTrajectoryAsync(query.TrajectoryId, cancellationToken);
        if (projection is null)
        {
            return QueryResult<PatientTrajectoryDto>.Failure("TRAJECTORY_NOT_FOUND", query.CorrelationId);
        }

        return QueryResult<PatientTrajectoryDto>.Ok(new PatientTrajectoryDto
        {
            TrajectoryId = projection.TrajectoryId,
            PatientId = projection.PatientId,
            QueueId = projection.QueueId,
            CurrentState = projection.CurrentState,
            OpenedAt = projection.OpenedAt,
            ClosedAt = projection.ClosedAt,
            CorrelationIds = projection.CorrelationIds,
            Stages = projection.Stages
                .OrderBy(stage => stage.OccurredAt)
                .Select(stage => new PatientTrajectoryStageDto
                {
                    OccurredAt = stage.OccurredAt,
                    Stage = stage.Stage,
                    SourceEvent = stage.SourceEvent,
                    SourceState = stage.SourceState,
                    CorrelationId = stage.CorrelationId
                })
                .ToArray()
        }, query.CorrelationId);
    }
}

public sealed class RebuildPatientTrajectoriesHandler : IRequestHandler<RebuildPatientTrajectoriesCommand, CommandResult<RebuildPatientTrajectoriesResultDto>>
{
    private static readonly ConcurrentDictionary<string, byte> RunningRebuilds = new(StringComparer.Ordinal);

    private readonly IEventStore _eventStore;
    private readonly IPatientTrajectoryRepository _trajectoryRepository;
    private readonly PatientTrajectoryProjectionWriter _projectionWriter;
    private readonly IAuditStore _auditStore;
    private readonly IPersistenceSession _persistenceSession;

    public RebuildPatientTrajectoriesHandler(
        IEventStore eventStore,
        IPatientTrajectoryRepository trajectoryRepository,
        PatientTrajectoryProjectionWriter projectionWriter,
        IAuditStore auditStore,
        IPersistenceSession persistenceSession)
    {
        _eventStore = eventStore;
        _trajectoryRepository = trajectoryRepository;
        _projectionWriter = projectionWriter;
        _auditStore = auditStore;
        _persistenceSession = persistenceSession;
    }

    public async Task<CommandResult<RebuildPatientTrajectoriesResultDto>> Handle(
        RebuildPatientTrajectoriesCommand command,
        CancellationToken cancellationToken)
    {
        var queueId = NormalizeFilter(command.QueueId);
        var patientId = NormalizeFilter(command.PatientId);

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return CommandResult<RebuildPatientTrajectoriesResultDto>.Failure(
                "TRAJECTORY_REBUILD_SCOPE_INVALID",
                command.CorrelationId);
        }

        var scope = BuildScope(queueId, patientId);
        var rebuildKey = $"{scope}|{command.IdempotencyKey}";

        if (!RunningRebuilds.TryAdd(rebuildKey, 0))
        {
            return CommandResult<RebuildPatientTrajectoriesResultDto>.Failure(
                "TRAJECTORY_REBUILD_ALREADY_RUNNING",
                command.CorrelationId);
        }

        try
        {
            var historicalEvents = (await _eventStore.GetAllAsync(cancellationToken))
                .Where(IsHistoricalTrajectoryEvent)
                .Where(@event => MatchesScope(@event, queueId, patientId))
                .OrderBy(@event => @event.OccurredAt)
                .ToList();

            var trajectories = RebuildFromHistoricalEvents(historicalEvents);

            if (!command.DryRun)
            {
                foreach (var trajectory in trajectories.Values.OrderBy(trajectory => trajectory.OpenedAt))
                {
                    if (await TrajectoryExistsAsync(trajectory.Id, cancellationToken))
                    {
                        await _projectionWriter.RefreshAsync(trajectory.Id, cancellationToken);
                        continue;
                    }

                    trajectory.RecordRebuild(scope, DateTime.UtcNow, command.CorrelationId);
                    await _trajectoryRepository.AddAsync(trajectory, cancellationToken);
                    await _projectionWriter.UpsertAsync(trajectory, cancellationToken);
                    trajectory.ClearUnraisedEvents();
                }
            }

            var response = new RebuildPatientTrajectoriesResultDto
            {
                JobId = $"TRJ-REBUILD-{Guid.NewGuid():N}",
                AcceptedAt = DateTime.UtcNow,
                Scope = scope,
                DryRun = command.DryRun,
                Status = command.DryRun ? "Completed" : "Accepted",
                EventsProcessed = historicalEvents.Count,
                TrajectoriesProcessed = trajectories.Count
            };

            await HandlerPersistence.CommitSuccessAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "REBUILD_PATIENT_TRAJECTORIES",
                "PatientTrajectory",
                scope,
                new
                {
                    Scope = scope,
                    command.DryRun,
                    response.EventsProcessed,
                    response.TrajectoriesProcessed,
                    command.IdempotencyKey
                },
                command.CorrelationId,
                cancellationToken);

            return CommandResult<RebuildPatientTrajectoriesResultDto>.Ok(
                response,
                command.CorrelationId,
                command.DryRun ? "Patient trajectory rebuild simulated successfully" : "Patient trajectory rebuild accepted");
        }
        catch (DomainException ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "REBUILD_PATIENT_TRAJECTORIES",
                "PatientTrajectory",
                scope,
                new { command.QueueId, command.PatientId, command.DryRun, command.IdempotencyKey },
                command.CorrelationId,
                ex.Message,
                cancellationToken);

            return CommandResult<RebuildPatientTrajectoriesResultDto>.Failure(ex, command.CorrelationId);
        }
        catch (Exception ex)
        {
            await HandlerPersistence.CommitFailureAsync(
                _persistenceSession,
                _auditStore,
                command.UserId,
                "REBUILD_PATIENT_TRAJECTORIES",
                "PatientTrajectory",
                scope,
                new { command.QueueId, command.PatientId, command.DryRun, command.IdempotencyKey },
                command.CorrelationId,
                ex.Message,
                cancellationToken);

            return CommandResult<RebuildPatientTrajectoriesResultDto>.Failure(
                $"Trajectory rebuild failed: {ex.Message}",
                command.CorrelationId);
        }
        finally
        {
            RunningRebuilds.TryRemove(rebuildKey, out _);
        }
    }

    private static Dictionary<string, PatientTrajectory> RebuildFromHistoricalEvents(IReadOnlyList<DomainEvent> events)
    {
        var trajectories = new Dictionary<string, PatientTrajectory>(StringComparer.Ordinal);
        var activeTrajectories = new Dictionary<string, PatientTrajectory>(StringComparer.Ordinal);
        var lastKnownQueueByPatient = new Dictionary<string, string>(StringComparer.Ordinal);
        var processedEventKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var @event in events)
        {
            if (!TryGetPatientId(@event, out var patientId))
            {
                continue;
            }

            var dedupKey = $"{@event.EventType}|{patientId}|{@event.CorrelationId}";
            if (!processedEventKeys.Add(dedupKey))
            {
                continue;
            }

            switch (@event)
            {
                case PatientCheckedIn checkedIn:
                    {
                        var queueId = checkedIn.AggregateId;
                        lastKnownQueueByPatient[patientId] = queueId;
                        var trajectory = PatientTrajectory.Start(
                            PatientTrajectoryIdFactory.Create(queueId, patientId, checkedIn.OccurredAt),
                            patientId,
                            queueId,
                            PatientTrajectory.ReceptionStage,
                            checkedIn.EventType,
                            "EnEsperaTaquilla",
                            checkedIn.OccurredAt,
                            checkedIn.CorrelationId);
                        trajectories[trajectory.Id] = trajectory;
                        activeTrajectories[BuildPatientQueueKey(patientId, queueId)] = trajectory;
                        break;
                    }

                case PatientPaymentValidated paymentValidated:
                    {
                        var queueId = paymentValidated.AggregateId;
                        lastKnownQueueByPatient[patientId] = queueId;
                        var trajectory = GetOrCreateHistoricalTrajectory(
                            trajectories,
                            activeTrajectories,
                            patientId,
                            queueId,
                            paymentValidated.OccurredAt,
                            PatientTrajectory.CashierStage,
                            paymentValidated.EventType,
                            "EnEsperaConsulta",
                            paymentValidated.CorrelationId);
                        trajectory.RecordStage(
                            PatientTrajectory.CashierStage,
                            paymentValidated.EventType,
                            "EnEsperaConsulta",
                            paymentValidated.OccurredAt,
                            paymentValidated.CorrelationId);
                        break;
                    }

                case PatientAbsentAtCashier absentAtCashier:
                    {
                        var queueId = absentAtCashier.AggregateId;
                        lastKnownQueueByPatient[patientId] = queueId;
                        var trajectory = GetOrCreateHistoricalTrajectory(
                            trajectories,
                            activeTrajectories,
                            patientId,
                            queueId,
                            absentAtCashier.OccurredAt,
                            PatientTrajectory.CashierStage,
                            absentAtCashier.EventType,
                            "CanceladoPorPago",
                            absentAtCashier.CorrelationId);
                        trajectory.Cancel(
                            absentAtCashier.EventType,
                            "CanceladoPorPago",
                            absentAtCashier.Reason,
                            absentAtCashier.OccurredAt,
                            absentAtCashier.CorrelationId);
                        activeTrajectories.Remove(BuildPatientQueueKey(patientId, queueId));
                        break;
                    }

                case PatientAttentionCompleted completed:
                    {
                        var queueId = ResolveQueueId(completed, activeTrajectories, lastKnownQueueByPatient);
                        if (queueId is null)
                        {
                            continue;
                        }

                        lastKnownQueueByPatient[patientId] = queueId;
                        var trajectory = GetOrCreateHistoricalTrajectory(
                            trajectories,
                            activeTrajectories,
                            patientId,
                            queueId,
                            completed.OccurredAt,
                            PatientTrajectory.ConsultationStage,
                            completed.EventType,
                            "Finalizado",
                            completed.CorrelationId);
                        trajectory.Complete(
                            PatientTrajectory.ConsultationStage,
                            completed.EventType,
                            "Finalizado",
                            completed.OccurredAt,
                            completed.CorrelationId);
                        activeTrajectories.Remove(BuildPatientQueueKey(patientId, queueId));
                        break;
                    }

                case PatientAbsentAtConsultation absentAtConsultation:
                    {
                        var queueId = ResolveQueueId(absentAtConsultation, activeTrajectories, lastKnownQueueByPatient);
                        if (queueId is null)
                        {
                            continue;
                        }

                        lastKnownQueueByPatient[patientId] = queueId;
                        var trajectory = GetOrCreateHistoricalTrajectory(
                            trajectories,
                            activeTrajectories,
                            patientId,
                            queueId,
                            absentAtConsultation.OccurredAt,
                            PatientTrajectory.ConsultationStage,
                            absentAtConsultation.EventType,
                            "CanceladoPorAusencia",
                            absentAtConsultation.CorrelationId);
                        trajectory.Cancel(
                            absentAtConsultation.EventType,
                            "CanceladoPorAusencia",
                            absentAtConsultation.Reason,
                            absentAtConsultation.OccurredAt,
                            absentAtConsultation.CorrelationId);
                        activeTrajectories.Remove(BuildPatientQueueKey(patientId, queueId));
                        break;
                    }
            }
        }

        return trajectories;
    }

    private static PatientTrajectory GetOrCreateHistoricalTrajectory(
        IDictionary<string, PatientTrajectory> trajectories,
        IDictionary<string, PatientTrajectory> activeTrajectories,
        string patientId,
        string queueId,
        DateTime occurredAt,
        string initialStage,
        string sourceEvent,
        string sourceState,
        string correlationId)
    {
        var patientQueueKey = BuildPatientQueueKey(patientId, queueId);
        if (activeTrajectories.TryGetValue(patientQueueKey, out var activeTrajectory))
        {
            return activeTrajectory;
        }

        var trajectory = PatientTrajectory.Start(
            PatientTrajectoryIdFactory.Create(queueId, patientId, occurredAt),
            patientId,
            queueId,
            initialStage,
            sourceEvent,
            sourceState,
            occurredAt,
            correlationId);
        trajectories[trajectory.Id] = trajectory;
        activeTrajectories[patientQueueKey] = trajectory;
        return trajectory;
    }

    private static string? ResolveQueueId(
        DomainEvent @event,
        IReadOnlyDictionary<string, PatientTrajectory> activeTrajectories,
        IReadOnlyDictionary<string, string> lastKnownQueueByPatient)
    {
        if (!TryGetPatientId(@event, out var patientId))
        {
            return null;
        }

        var activeTrajectory = activeTrajectories
            .Values
            .Where(trajectory => string.Equals(trajectory.PatientId, patientId, StringComparison.Ordinal))
            .OrderByDescending(trajectory => trajectory.OpenedAt)
            .FirstOrDefault();

        if (activeTrajectory is not null)
        {
            return activeTrajectory.QueueId;
        }

        if (lastKnownQueueByPatient.TryGetValue(patientId, out var queueId))
        {
            return queueId;
        }

        return @event.AggregateId;
    }

    private async Task<bool> TrajectoryExistsAsync(string trajectoryId, CancellationToken cancellationToken)
    {
        try
        {
            await _trajectoryRepository.GetByIdAsync(trajectoryId, cancellationToken);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    private static bool IsHistoricalTrajectoryEvent(DomainEvent @event) => @event switch
    {
        PatientCheckedIn => true,
        PatientPaymentValidated => true,
        PatientAttentionCompleted => true,
        PatientAbsentAtCashier => true,
        PatientAbsentAtConsultation => true,
        _ => false
    };

    private static bool MatchesScope(DomainEvent @event, string? queueId, string? patientId)
    {
        if (patientId is not null && (!TryGetPatientId(@event, out var eventPatientId) || !string.Equals(eventPatientId, patientId, StringComparison.Ordinal)))
        {
            return false;
        }

        if (queueId is null)
        {
            return true;
        }

        return @event switch
        {
            PatientCheckedIn checkedIn => string.Equals(checkedIn.AggregateId, queueId, StringComparison.Ordinal),
            PatientPaymentValidated paymentValidated => string.Equals(paymentValidated.AggregateId, queueId, StringComparison.Ordinal),
            PatientAbsentAtCashier absentAtCashier => string.Equals(absentAtCashier.AggregateId, queueId, StringComparison.Ordinal),
            PatientAttentionCompleted completed => string.Equals(completed.AggregateId, queueId, StringComparison.Ordinal),
            PatientAbsentAtConsultation absentAtConsultation => string.Equals(absentAtConsultation.AggregateId, queueId, StringComparison.Ordinal),
            _ => false
        };
    }

    private static bool TryGetPatientId(DomainEvent @event, out string patientId)
    {
        patientId = @event switch
        {
            PatientCheckedIn checkedIn => checkedIn.PatientId,
            PatientPaymentValidated paymentValidated => paymentValidated.PatientId,
            PatientAttentionCompleted completed => completed.PatientId,
            PatientAbsentAtCashier absentAtCashier => absentAtCashier.PatientId,
            PatientAbsentAtConsultation absentAtConsultation => absentAtConsultation.PatientId,
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(patientId);
    }

    private static string BuildScope(string? queueId, string? patientId)
    {
        if (queueId is null && patientId is null)
        {
            return "all";
        }

        if (queueId is null)
        {
            return $"patient:{patientId}";
        }

        if (patientId is null)
        {
            return $"queue:{queueId}";
        }

        return $"queue:{queueId}|patient:{patientId}";
    }

    private static string? NormalizeFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string BuildPatientQueueKey(string patientId, string queueId)
        => $"{patientId}|{queueId}";
}
