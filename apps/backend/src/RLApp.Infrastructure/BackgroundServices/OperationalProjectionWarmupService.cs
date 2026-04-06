using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RLApp.Adapters.Persistence.Data;
using RLApp.Domain.Common;
using RLApp.Domain.Events;
using RLApp.Ports.Outbound;

namespace RLApp.Infrastructure.BackgroundServices;

public sealed class OperationalProjectionWarmupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OperationalProjectionWarmupService> _logger;

    public OperationalProjectionWarmupService(
        IServiceProvider serviceProvider,
        ILogger<OperationalProjectionWarmupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (!await ShouldRebuildAsync(dbContext, cancellationToken))
            {
                _logger.LogInformation("Operational projections are current. Startup warmup skipped.");
                return;
            }

            _logger.LogInformation("Operational projections are stale. Starting startup warmup from the event store.");

            var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
            var projectionStore = scope.ServiceProvider.GetRequiredService<IProjectionStore>();
            var snapshot = BuildSnapshot(await eventStore.GetAllAsync(cancellationToken));

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            dbContext.QueueStates.RemoveRange(dbContext.QueueStates);
            dbContext.WaitingRoomMonitors.RemoveRange(dbContext.WaitingRoomMonitors);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var queueState in snapshot.QueueStates.Values.OrderBy(state => state.QueueId))
            {
                await projectionStore.UpsertAsync(
                    queueState.QueueId,
                    "QueueState",
                    new Dictionary<string, object>
                    {
                        ["TotalPending"] = queueState.TotalPending,
                        ["AverageWaitTimeMinutes"] = queueState.AverageWaitTimeMinutes
                    },
                    cancellationToken);
            }

            foreach (var monitor in snapshot.Monitors.Values.OrderBy(state => state.TurnId))
            {
                var projectionData = new Dictionary<string, object>
                {
                    ["QueueId"] = monitor.QueueId,
                    ["PatientId"] = monitor.PatientId,
                    ["TurnId"] = monitor.TurnId,
                    ["PatientName"] = monitor.PatientName,
                    ["TicketNumber"] = monitor.TicketNumber,
                    ["CheckedInAt"] = monitor.CheckedInAt,
                    ["UpdatedAt"] = monitor.UpdatedAt,
                    ["Status"] = monitor.Status
                };

                if (!string.IsNullOrWhiteSpace(monitor.RoomAssigned))
                {
                    projectionData["RoomAssigned"] = monitor.RoomAssigned!;
                }

                await projectionStore.UpsertAsync(
                    monitor.TurnId,
                    "WaitingRoomMonitor",
                    projectionData,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Operational projection warmup completed. QueueStates={QueueCount}, WaitingRoomMonitors={MonitorCount}.",
                snapshot.QueueStates.Count,
                snapshot.Monitors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Operational projection warmup skipped because the persistence schema is not ready yet.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<bool> ShouldRebuildAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var latestEventAt = await dbContext.EventStore
            .AsNoTracking()
            .OrderByDescending(record => record.OccurredAt)
            .Select(record => (DateTime?)record.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (!latestEventAt.HasValue)
        {
            return false;
        }

        var latestQueueUpdate = await dbContext.QueueStates
            .AsNoTracking()
            .Select(queue => (DateTime?)queue.LastUpdatedAt)
            .MaxAsync(cancellationToken);

        var latestMonitorUpdate = await dbContext.WaitingRoomMonitors
            .AsNoTracking()
            .Select(monitor => (DateTime?)monitor.UpdatedAt)
            .MaxAsync(cancellationToken);

        var monitorSchemaDriftDetected = await dbContext.WaitingRoomMonitors
            .AsNoTracking()
            .AnyAsync(
                monitor => string.IsNullOrWhiteSpace(monitor.QueueId)
                    || string.IsNullOrWhiteSpace(monitor.PatientId)
                    || monitor.CheckedInAt == default,
                cancellationToken);

        if (monitorSchemaDriftDetected)
        {
            return true;
        }

        var latestProjectionUpdate = new[] { latestQueueUpdate, latestMonitorUpdate }
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();

        return latestProjectionUpdate == DateTime.MinValue || latestProjectionUpdate < latestEventAt.Value;
    }

    private static OperationalProjectionSnapshot BuildSnapshot(IList<DomainEvent> domainEvents)
    {
        var queueStates = new Dictionary<string, QueueStateSnapshot>(StringComparer.Ordinal);
        var monitors = new Dictionary<string, WaitingRoomMonitorSnapshot>(StringComparer.Ordinal);
        var activePatients = new Dictionary<string, ActivePatientSnapshot>(StringComparer.Ordinal);

        foreach (var domainEvent in domainEvents.OrderBy(item => item.OccurredAt))
        {
            switch (domainEvent)
            {
                case WaitingQueueCreated created:
                    EnsureQueue(queueStates, created.AggregateId);
                    break;

                case PatientCheckedIn checkedIn:
                {
                    var queueId = checkedIn.AggregateId;
                    var turnId = BuildTurnId(queueId, checkedIn.PatientId);

                    EnsureQueue(queueStates, queueId).TotalPending++;
                    activePatients[checkedIn.PatientId] = new ActivePatientSnapshot(queueId, turnId);
                    monitors[turnId] = new WaitingRoomMonitorSnapshot(
                        turnId,
                        queueId,
                        checkedIn.PatientId,
                        checkedIn.PatientName,
                        turnId,
                        checkedIn.OccurredAt,
                        "Waiting",
                        null,
                        checkedIn.OccurredAt);
                    break;
                }

                case PatientCalled called:
                    UpdateMonitor(activePatients, monitors, called.PatientId, "Called", called.RoomId, called.OccurredAt);
                    break;

                case PatientClaimedForAttention claimed:
                    UpdateMonitor(activePatients, monitors, claimed.PatientId, "InConsultation", claimed.RoomId, claimed.OccurredAt);
                    break;

                case PatientAttentionCompleted completed:
                    CompletePatient(activePatients, queueStates, monitors, completed.PatientId, completed.TurnId, "Completed", completed.RoomId, completed.OccurredAt);
                    break;

                case PatientAbsentAtConsultation absentAtConsultation:
                    CompletePatient(activePatients, queueStates, monitors, absentAtConsultation.PatientId, absentAtConsultation.TurnId, "Absent", null, absentAtConsultation.OccurredAt);
                    break;

                case PatientAbsentAtCashier absentAtCashier:
                    CompletePatient(activePatients, queueStates, monitors, absentAtCashier.PatientId, absentAtCashier.TurnId, "Absent", null, absentAtCashier.OccurredAt);
                    break;

                case PatientCancelledByPayment cancelledByPayment:
                    CompletePatient(activePatients, queueStates, monitors, cancelledByPayment.PatientId, null, "Cancelled", null, cancelledByPayment.OccurredAt);
                    break;

                case PatientCancelledByAbsence cancelledByAbsence:
                    CompletePatient(activePatients, queueStates, monitors, cancelledByAbsence.PatientId, null, "Cancelled", null, cancelledByAbsence.OccurredAt);
                    break;
            }
        }

        return new OperationalProjectionSnapshot(queueStates, monitors);
    }

    private static QueueStateSnapshot EnsureQueue(IDictionary<string, QueueStateSnapshot> queueStates, string queueId)
    {
        if (!queueStates.TryGetValue(queueId, out var queueState))
        {
            queueState = new QueueStateSnapshot(queueId);
            queueStates[queueId] = queueState;
        }

        return queueState;
    }

    private static void UpdateMonitor(
        Dictionary<string, ActivePatientSnapshot> activePatients,
        IDictionary<string, WaitingRoomMonitorSnapshot> monitors,
        string patientId,
        string status,
        string? roomAssigned,
        DateTime updatedAt)
    {
        if (!activePatients.TryGetValue(patientId, out var patient))
        {
            return;
        }

        if (!monitors.TryGetValue(patient.TurnId, out var monitor))
        {
            monitor = new WaitingRoomMonitorSnapshot(
                patient.TurnId,
                patient.QueueId,
                patientId,
                patientId,
                patient.TurnId,
                updatedAt,
                "Waiting",
                null,
                updatedAt);
        }

        monitors[patient.TurnId] = monitor with
        {
            Status = status,
            RoomAssigned = roomAssigned ?? monitor.RoomAssigned,
            UpdatedAt = updatedAt
        };
    }

    private static void CompletePatient(
        Dictionary<string, ActivePatientSnapshot> activePatients,
        IDictionary<string, QueueStateSnapshot> queueStates,
        IDictionary<string, WaitingRoomMonitorSnapshot> monitors,
        string patientId,
        string? turnId,
        string status,
        string? roomAssigned,
        DateTime updatedAt)
    {
        if (activePatients.TryGetValue(patientId, out var patient))
        {
            UpdateMonitor(activePatients, monitors, patientId, status, roomAssigned, updatedAt);

            var queueState = EnsureQueue(queueStates, patient.QueueId);
            if (queueState.TotalPending > 0)
            {
                queueState.TotalPending--;
            }

            activePatients.Remove(patientId);
            return;
        }

        if (string.IsNullOrWhiteSpace(turnId) || !monitors.TryGetValue(turnId, out var existingMonitor))
        {
            return;
        }

        monitors[turnId] = existingMonitor with
        {
            Status = status,
            RoomAssigned = roomAssigned ?? existingMonitor.RoomAssigned,
            UpdatedAt = updatedAt
        };
    }

    private static string BuildTurnId(string queueId, string patientId) => $"{queueId}-{patientId}";

    private sealed record OperationalProjectionSnapshot(
        Dictionary<string, QueueStateSnapshot> QueueStates,
        Dictionary<string, WaitingRoomMonitorSnapshot> Monitors);

    private sealed record ActivePatientSnapshot(string QueueId, string TurnId);

    private sealed record WaitingRoomMonitorSnapshot(
        string TurnId,
        string QueueId,
        string PatientId,
        string PatientName,
        string TicketNumber,
        DateTime CheckedInAt,
        string Status,
        string? RoomAssigned,
        DateTime UpdatedAt);

    private sealed class QueueStateSnapshot
    {
        public QueueStateSnapshot(string queueId)
        {
            QueueId = queueId;
        }

        public string QueueId { get; }
        public int TotalPending { get; set; }
        public double AverageWaitTimeMinutes { get; set; }
    }
}