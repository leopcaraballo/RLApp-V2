using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
using RLApp.Domain.Aggregates;
using RLApp.Ports.Outbound;

namespace RLApp.Adapters.Persistence.Repositories;

/// <summary>
/// Implements the projection store for read models (materialized views)
/// Updates materialized views in response to domain events via outbox processor
/// Implements S-006: Public Display & Read Models, S-008: Event Sourcing, ADR-006
/// </summary>
public class ProjectionStoreRepository : IProjectionStore
{
    private readonly AppDbContext _context;

    public ProjectionStoreRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task UpsertAsync(string projectionId, string projectionType, object projectionData,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(projectionData);

        switch (projectionType)
        {
            case "QueueState":
                await UpsertQueueStateAsync(projectionId, projectionData, cancellationToken);
                break;

            case "WaitingRoomMonitor":
                await UpsertWaitingRoomAsync(projectionId, projectionData, cancellationToken);
                break;

            case "Dashboard":
                await UpsertDashboardAsync(projectionData, cancellationToken);
                break;

            case "PatientTrajectory":
                await UpsertPatientTrajectoryAsync(projectionId, projectionData, cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"Unknown projection type: {projectionType}");
        }
    }

    public async Task<T> GetAsync<T>(string projectionId, CancellationToken cancellationToken = default)
        where T : class
    {
        // Generic get using type name from T
        var typeName = typeof(T).Name;

        object? result = typeName switch
        {
            "QueueStateView" => await _context.QueueStates
                .FirstOrDefaultAsync(q => q.QueueId == projectionId, cancellationToken),

            "WaitingRoomMonitorView" => await _context.WaitingRoomMonitors
                .FirstOrDefaultAsync(w => w.TurnId == projectionId, cancellationToken),

            "OperationsDashboardView" => await _context.OperationsDashboards
                .FirstOrDefaultAsync(d => d.Id == "SYSTEM_SINGLETON", cancellationToken),

            "PatientTrajectoryView" => await _context.PatientTrajectories
                .FirstOrDefaultAsync(t => t.TrajectoryId == projectionId, cancellationToken),

            _ => throw new InvalidOperationException($"Unknown projection type: {typeName}")
        };

        return result as T ?? throw new KeyNotFoundException($"Projection not found: {projectionId}");
    }

    public async Task<IReadOnlyList<PatientTrajectoryProjection>> FindPatientTrajectoriesAsync(
        string patientId,
        string? queueId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PatientTrajectories
            .AsNoTracking()
            .Where(trajectory => trajectory.PatientId == patientId);

        if (!string.IsNullOrWhiteSpace(queueId))
        {
            query = query.Where(trajectory => trajectory.QueueId == queueId);
        }

        var views = await query
            .OrderByDescending(trajectory => trajectory.CurrentState == PatientTrajectory.ActiveState)
            .ThenByDescending(trajectory => trajectory.OpenedAt)
            .ToListAsync(cancellationToken);

        return views
            .Select(MapToProjection)
            .ToArray();
    }

    public async Task<PatientTrajectoryProjection?> GetPatientTrajectoryAsync(string trajectoryId, CancellationToken cancellationToken = default)
    {
        var view = await _context.PatientTrajectories
            .AsNoTracking()
            .FirstOrDefaultAsync(trajectory => trajectory.TrajectoryId == trajectoryId, cancellationToken);

        if (view is null)
        {
            return null;
        }

        return MapToProjection(view);
    }

    public async Task<IReadOnlyList<PatientTrajectoryProjection>> QueryPatientTrajectoriesAsync(
        string queueId,
        string? state,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PatientTrajectories
            .AsNoTracking()
            .Where(t => t.QueueId == queueId);

        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(t => t.CurrentState == state);
        }

        if (from.HasValue)
        {
            query = query.Where(t => t.OpenedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(t => t.OpenedAt <= to.Value);
        }

        var views = await query
            .OrderByDescending(t => t.OpenedAt)
            .ToListAsync(cancellationToken);

        return views.Select(MapToProjection).ToArray();
    }

    private static PatientTrajectoryProjection MapToProjection(PatientTrajectoryView view) => new()
    {
        TrajectoryId = view.TrajectoryId,
        PatientId = view.PatientId,
        QueueId = view.QueueId,
        CurrentState = view.CurrentState,
        OpenedAt = view.OpenedAt,
        ClosedAt = view.ClosedAt,
        CorrelationIds = Deserialize<List<string>>(view.CorrelationIdsJson),
        Stages = Deserialize<List<PatientTrajectoryStageProjection>>(view.StagesJson)
    };

    public async Task<WaitingRoomMonitorProjection?> GetWaitingRoomMonitorAsync(string queueId, CancellationToken cancellationToken = default)
    {
        var normalizedQueueId = queueId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQueueId))
        {
            return null;
        }

        var queueState = await _context.QueueStates
            .AsNoTracking()
            .FirstOrDefaultAsync(queue => queue.QueueId == normalizedQueueId, cancellationToken);

        var monitors = await _context.WaitingRoomMonitors
            .AsNoTracking()
            .Where(monitor => monitor.QueueId == normalizedQueueId)
            .OrderByDescending(monitor => monitor.UpdatedAt)
            .ToListAsync(cancellationToken);

        if (queueState is null && monitors.Count == 0)
        {
            return null;
        }

        var generatedAt = monitors.Count == 0
            ? queueState?.LastUpdatedAt ?? DateTime.UtcNow
            : MaxDate(queueState?.LastUpdatedAt, monitors.Max(monitor => monitor.UpdatedAt));

        var waitingEntries = monitors.Where(monitor => OperationalVisibleStatuses.CountsAsWaiting(monitor.Status)).ToArray();
        var inConsultationEntries = monitors.Where(monitor => OperationalVisibleStatuses.CountsAsActiveConsultation(monitor.Status)).ToArray();

        return new WaitingRoomMonitorProjection
        {
            QueueId = normalizedQueueId,
            GeneratedAt = generatedAt,
            WaitingCount = waitingEntries.Length,
            AverageWaitTimeMinutes = CalculateAverageWaitMinutes(waitingEntries),
            ActiveConsultationRooms = inConsultationEntries
                .Select(monitor => monitor.RoomAssigned)
                .Where(roomAssigned => !string.IsNullOrWhiteSpace(roomAssigned))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            StatusBreakdown = BuildStatusBreakdown(monitors),
            Entries = monitors.Select(monitor => new WaitingRoomMonitorEntryProjection
            {
                TurnId = monitor.TurnId,
                PatientId = monitor.PatientId,
                PatientName = monitor.PatientName,
                TicketNumber = monitor.TicketNumber,
                Status = monitor.Status,
                RoomAssigned = monitor.RoomAssigned,
                CheckedInAt = monitor.CheckedInAt,
                UpdatedAt = monitor.UpdatedAt
            }).ToArray()
        };
    }

    public async Task<OperationsDashboardProjection> GetOperationsDashboardAsync(CancellationToken cancellationToken = default)
    {
        var dashboard = await _context.OperationsDashboards
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var queueStates = await _context.QueueStates
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var monitors = await _context.WaitingRoomMonitors
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var queueIds = queueStates.Select(queue => queue.QueueId)
            .Concat(monitors.Select(monitor => monitor.QueueId))
            .Where(queueId => !string.IsNullOrWhiteSpace(queueId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(queueId => queueId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var queueSnapshots = queueIds
            .Select(queueId =>
            {
                var queue = queueStates.FirstOrDefault(item => string.Equals(item.QueueId, queueId, StringComparison.OrdinalIgnoreCase));
                var queueMonitors = monitors.Where(monitor => string.Equals(monitor.QueueId, queueId, StringComparison.OrdinalIgnoreCase)).ToArray();
                var waitingEntries = queueMonitors.Where(monitor => OperationalVisibleStatuses.CountsAsWaiting(monitor.Status)).ToArray();

                return new DashboardQueueSnapshotProjection
                {
                    QueueId = queueId,
                    TotalPending = waitingEntries.Length > 0 ? waitingEntries.Length : queue?.TotalPending ?? 0,
                    AverageWaitTimeMinutes = queue?.AverageWaitTimeMinutes > 0
                        ? queue.AverageWaitTimeMinutes
                        : CalculateAverageWaitMinutes(waitingEntries),
                    LastUpdatedAt = MaxDate(
                        queue?.LastUpdatedAt,
                        queueMonitors.Select(monitor => (DateTime?)monitor.UpdatedAt).DefaultIfEmpty().Max())
                };
            })
            .ToArray();

        var latestProjectionAt = new[]
        {
            queueSnapshots.Select(snapshot => (DateTime?)snapshot.LastUpdatedAt).DefaultIfEmpty().Max(),
            monitors.Select(monitor => (DateTime?)monitor.UpdatedAt).DefaultIfEmpty().Max()
        }
        .Where(value => value.HasValue)
        .Select(value => value!.Value)
        .DefaultIfEmpty(DateTime.UtcNow)
        .Max();

        var latestEventAt = await _context.EventStore
            .AsNoTracking()
            .OrderByDescending(record => record.OccurredAt)
            .Select(record => (DateTime?)record.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);

        var activeRooms = Math.Max(
            dashboard?.ActiveRooms ?? 0,
            monitors
                .Where(monitor => OperationalVisibleStatuses.CountsAsActiveConsultation(monitor.Status))
                .Select(monitor => monitor.RoomAssigned)
                .Where(roomAssigned => !string.IsNullOrWhiteSpace(roomAssigned))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());

        var today = DateTime.UtcNow.Date;
        var derivedTotalPatientsToday = monitors.Count(monitor => monitor.CheckedInAt.Date == today);
        var derivedTotalCompleted = monitors.Count(monitor => string.Equals(monitor.Status, OperationalVisibleStatuses.Completed, StringComparison.OrdinalIgnoreCase) && monitor.UpdatedAt.Date == today);

        return new OperationsDashboardProjection
        {
            GeneratedAt = latestProjectionAt,
            CurrentWaitingCount = queueSnapshots.Sum(snapshot => snapshot.TotalPending),
            AverageWaitTimeMinutes = queueSnapshots.Length == 0
                ? 0
                : Math.Round(queueSnapshots.Average(snapshot => snapshot.AverageWaitTimeMinutes), 2),
            TotalPatientsToday = Math.Max(dashboard?.TotalPatientsToday ?? 0, derivedTotalPatientsToday),
            TotalCompleted = Math.Max(dashboard?.TotalCompleted ?? 0, derivedTotalCompleted),
            ActiveRooms = activeRooms,
            ProjectionLagSeconds = CalculateProjectionLagSeconds(latestProjectionAt, latestEventAt),
            QueueSnapshots = queueSnapshots,
            StatusBreakdown = BuildStatusBreakdown(monitors)
        };
    }

    public async Task DeleteAsync(string projectionId, CancellationToken cancellationToken = default)
    {
        // Remove from all possible views
        var queueState = await _context.QueueStates
            .FirstOrDefaultAsync(q => q.QueueId == projectionId, cancellationToken);
        if (queueState != null)
        {
            _context.QueueStates.Remove(queueState);
        }

        var waitingRoom = await _context.WaitingRoomMonitors
            .FirstOrDefaultAsync(w => w.TurnId == projectionId, cancellationToken);
        if (waitingRoom != null)
        {
            _context.WaitingRoomMonitors.Remove(waitingRoom);
        }

        var trajectory = await _context.PatientTrajectories
            .FirstOrDefaultAsync(t => t.TrajectoryId == projectionId, cancellationToken);
        if (trajectory != null)
        {
            _context.PatientTrajectories.Remove(trajectory);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertQueueStateAsync(string queueId, object projectionData,
        CancellationToken cancellationToken)
    {
        if (projectionData is not System.Collections.Generic.IDictionary<string, object> dict)
            throw new InvalidOperationException("Projection data must be a dictionary");

        var existing = await _context.QueueStates
            .FirstOrDefaultAsync(q => q.QueueId == queueId, cancellationToken);

        if (existing != null)
        {
            if (dict.TryGetValue("TotalPending", out var p)) existing.TotalPending = Convert.ToInt32(p);
            if (dict.TryGetValue("AverageWaitTimeMinutes", out var w)) existing.AverageWaitTimeMinutes = Convert.ToDouble(w);
            existing.LastUpdatedAt = DateTime.UtcNow;
            _context.QueueStates.Update(existing);
        }
        else
        {
            var newQueue = new QueueStateView
            {
                QueueId = queueId,
                TotalPending = Convert.ToInt32(dict.TryGetValue("TotalPending", out var p) ? p : 0),
                AverageWaitTimeMinutes = Convert.ToDouble(dict.TryGetValue("AverageWaitTimeMinutes", out var w) ? w : 0.0),
                LastUpdatedAt = DateTime.UtcNow
            };
            await _context.QueueStates.AddAsync(newQueue, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertWaitingRoomAsync(string turnId, object projectionData,
        CancellationToken cancellationToken)
    {
        if (projectionData is not System.Collections.Generic.IDictionary<string, object> dict)
            throw new InvalidOperationException("Projection data must be a dictionary");

        var patientId = ReadString(dict, "PatientId") ?? turnId;
        var queueId = ReadString(dict, "QueueId");
        var providedTurnId = ReadString(dict, "TurnId") ?? turnId;

        var existing = await _context.WaitingRoomMonitors
            .FirstOrDefaultAsync(w => w.TurnId == providedTurnId, cancellationToken);

        if (existing is null)
        {
            existing = await _context.WaitingRoomMonitors
                .FirstOrDefaultAsync(w => w.PatientId == patientId, cancellationToken);
        }

        var effectiveQueueId = queueId
            ?? existing?.QueueId
            ?? ExtractQueueId(providedTurnId)
            ?? string.Empty;
        var effectiveTurnId = existing?.TurnId
            ?? providedTurnId
            ?? BuildTurnId(effectiveQueueId, patientId);
        var checkedInAt = ReadDateTime(dict, "CheckedInAt") ?? existing?.CheckedInAt ?? DateTime.UtcNow;
        var updatedAt = ReadDateTime(dict, "UpdatedAt") ?? DateTime.UtcNow;

        if (existing != null)
        {
            existing.QueueId = effectiveQueueId;
            existing.PatientId = patientId;
            if (dict.TryGetValue("PatientName", out var pn) && pn != null) existing.PatientName = pn.ToString()!;
            if (dict.TryGetValue("TicketNumber", out var tn) && tn != null) existing.TicketNumber = tn.ToString()!;
            if (dict.TryGetValue("Status", out var s) && s != null)
            {
                existing.Status = OperationalVisibleStatuses.ResolveNextStatus(existing.Status, s.ToString());
            }
            if (dict.TryGetValue("RoomAssigned", out var ra)) existing.RoomAssigned = ra?.ToString();

            existing.CheckedInAt = checkedInAt;
            existing.UpdatedAt = updatedAt;
            _context.WaitingRoomMonitors.Update(existing);
        }
        else
        {
            var newMonitor = new WaitingRoomMonitorView
            {
                TurnId = effectiveTurnId,
                QueueId = effectiveQueueId,
                PatientId = patientId,
                PatientName = dict.TryGetValue("PatientName", out var pn) ? pn?.ToString() ?? "Unknown" : "Unknown",
                TicketNumber = dict.TryGetValue("TicketNumber", out var tn) ? tn?.ToString() ?? effectiveTurnId : effectiveTurnId,
                Status = dict.TryGetValue("Status", out var s)
                    ? OperationalVisibleStatuses.Normalize(s?.ToString())
                    : OperationalVisibleStatuses.Waiting,
                RoomAssigned = dict.TryGetValue("RoomAssigned", out var ra) ? ra?.ToString() : null,
                CheckedInAt = checkedInAt,
                UpdatedAt = updatedAt
            };
            await _context.WaitingRoomMonitors.AddAsync(newMonitor, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertDashboardAsync(object projectionData, CancellationToken cancellationToken)
    {
        if (projectionData is not System.Collections.Generic.IDictionary<string, object> dict)
            throw new InvalidOperationException("Projection data must be a dictionary");

        var dashboard = await _context.OperationsDashboards
            .FirstOrDefaultAsync(cancellationToken);

        if (dashboard != null)
        {
            if (dict.TryGetValue("TotalPatientsToday", out var t)) dashboard.TotalPatientsToday = Convert.ToInt32(t);
            if (dict.TryGetValue("ActiveRooms", out var ar)) dashboard.ActiveRooms = Convert.ToInt32(ar);
            if (dict.TryGetValue("TotalCompleted", out var c)) dashboard.TotalCompleted = Convert.ToInt32(c);
            dashboard.Date = DateTime.UtcNow.Date;
            _context.OperationsDashboards.Update(dashboard);
        }
        else
        {
            var newDashboard = new OperationsDashboardView
            {
                Id = "SYSTEM_SINGLETON",
                TotalPatientsToday = Convert.ToInt32(dict.TryGetValue("TotalPatientsToday", out var t) ? t : 0),
                ActiveRooms = Convert.ToInt32(dict.TryGetValue("ActiveRooms", out var ar) ? ar : 0),
                TotalCompleted = Convert.ToInt32(dict.TryGetValue("TotalCompleted", out var c) ? c : 0),
                Date = DateTime.UtcNow.Date
            };
            await _context.OperationsDashboards.AddAsync(newDashboard, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertPatientTrajectoryAsync(string trajectoryId, object projectionData, CancellationToken cancellationToken)
    {
        if (projectionData is not PatientTrajectoryProjection trajectoryProjection)
            throw new InvalidOperationException("Projection data must be a patient trajectory projection");

        var existing = await _context.PatientTrajectories
            .FirstOrDefaultAsync(trajectory => trajectory.TrajectoryId == trajectoryId, cancellationToken);

        var correlationIdsJson = JsonSerializer.Serialize(trajectoryProjection.CorrelationIds);
        var stagesJson = JsonSerializer.Serialize(trajectoryProjection.Stages);

        if (existing is null)
        {
            existing = new PatientTrajectoryView
            {
                TrajectoryId = trajectoryProjection.TrajectoryId,
                PatientId = trajectoryProjection.PatientId,
                QueueId = trajectoryProjection.QueueId,
                CurrentState = trajectoryProjection.CurrentState,
                OpenedAt = trajectoryProjection.OpenedAt,
                ClosedAt = trajectoryProjection.ClosedAt,
                CorrelationIdsJson = correlationIdsJson,
                StagesJson = stagesJson,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.PatientTrajectories.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.PatientId = trajectoryProjection.PatientId;
            existing.QueueId = trajectoryProjection.QueueId;
            existing.CurrentState = trajectoryProjection.CurrentState;
            existing.OpenedAt = trajectoryProjection.OpenedAt;
            existing.ClosedAt = trajectoryProjection.ClosedAt;
            existing.CorrelationIdsJson = correlationIdsJson;
            existing.StagesJson = stagesJson;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.PatientTrajectories.Update(existing);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static T Deserialize<T>(string payload) where T : new()
        => JsonSerializer.Deserialize<T>(payload) ?? new T();

    private static string? ReadString(System.Collections.Generic.IDictionary<string, object> dict, string key)
        => dict.TryGetValue(key, out var value) ? value?.ToString() : null;

    private static DateTime? ReadDateTime(System.Collections.Generic.IDictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            DateTime dateTime => dateTime,
            DateTimeOffset dateTimeOffset => dateTimeOffset.UtcDateTime,
            string text when DateTime.TryParse(text, out var parsed) => parsed,
            _ => null
        };
    }

    private static string? ExtractQueueId(string turnId)
    {
        if (string.IsNullOrWhiteSpace(turnId))
        {
            return null;
        }

        var marker = "-PAT-";
        var markerIndex = turnId.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex <= 0)
        {
            return null;
        }

        return turnId[..markerIndex];
    }

    private static string BuildTurnId(string queueId, string patientId)
        => string.IsNullOrWhiteSpace(queueId) ? patientId : $"{queueId}-{patientId}";

    private static IReadOnlyList<OperationalStatusCountProjection> BuildStatusBreakdown(IEnumerable<WaitingRoomMonitorView> monitors)
        => monitors
            .GroupBy(monitor => OperationalVisibleStatuses.Normalize(monitor.Status), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new OperationalStatusCountProjection
            {
                Status = group.Key,
                Total = group.Count()
            })
            .ToArray();

    private static double CalculateAverageWaitMinutes(IEnumerable<WaitingRoomMonitorView> entries)
    {
        var activeEntries = entries.ToArray();
        if (activeEntries.Length == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        var average = activeEntries.Average(entry => Math.Max(0, (now - entry.CheckedInAt).TotalMinutes));
        return Math.Round(average, 2);
    }

    private static DateTime MaxDate(DateTime? left, DateTime? right)
    {
        if (!left.HasValue)
        {
            return right ?? DateTime.UtcNow;
        }

        if (!right.HasValue)
        {
            return left.Value;
        }

        return left.Value >= right.Value ? left.Value : right.Value;
    }

    private static int CalculateProjectionLagSeconds(DateTime latestProjectionAt, DateTime? latestEventAt)
    {
        if (!latestEventAt.HasValue)
        {
            return 0;
        }

        var lag = latestEventAt.Value - latestProjectionAt;
        if (lag <= TimeSpan.Zero)
        {
            return 0;
        }

        return (int)Math.Round(lag.TotalSeconds);
    }
}
