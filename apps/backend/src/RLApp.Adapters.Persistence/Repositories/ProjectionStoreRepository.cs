using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RLApp.Adapters.Persistence.Data;
using RLApp.Adapters.Persistence.Data.Models;
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

    public async Task<PatientTrajectoryProjection?> GetPatientTrajectoryAsync(string trajectoryId, CancellationToken cancellationToken = default)
    {
        var view = await _context.PatientTrajectories
            .AsNoTracking()
            .FirstOrDefaultAsync(trajectory => trajectory.TrajectoryId == trajectoryId, cancellationToken);

        if (view is null)
        {
            return null;
        }

        return new PatientTrajectoryProjection
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

        var existing = await _context.WaitingRoomMonitors
            .FirstOrDefaultAsync(w => w.TurnId == turnId, cancellationToken);

        if (existing != null)
        {
            if (dict.TryGetValue("PatientName", out var pn) && pn != null) existing.PatientName = pn.ToString()!;
            if (dict.TryGetValue("TicketNumber", out var tn) && tn != null) existing.TicketNumber = tn.ToString()!;
            if (dict.TryGetValue("Status", out var s) && s != null) existing.Status = s.ToString()!;
            if (dict.TryGetValue("RoomAssigned", out var ra)) existing.RoomAssigned = ra?.ToString();

            existing.UpdatedAt = DateTime.UtcNow;
            _context.WaitingRoomMonitors.Update(existing);
        }
        else
        {
            var newMonitor = new WaitingRoomMonitorView
            {
                TurnId = turnId,
                PatientName = dict.TryGetValue("PatientName", out var pn) ? pn?.ToString() ?? "Unknown" : "Unknown",
                TicketNumber = dict.TryGetValue("TicketNumber", out var tn) ? tn?.ToString() ?? turnId : turnId,
                Status = dict.TryGetValue("Status", out var s) ? s?.ToString() ?? "Waiting" : "Waiting",
                RoomAssigned = dict.TryGetValue("RoomAssigned", out var ra) ? ra?.ToString() : null,
                UpdatedAt = DateTime.UtcNow
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
}
