using Microsoft.Extensions.Logging;
using RLApp.Application.Services;
using RLApp.Domain.Events;
using RLApp.Ports.Outbound;

namespace RLApp.Infrastructure.BackgroundServices;

public sealed class LocalOutboxMessageDispatcher : IOutboxMessageDispatcher
{
    private readonly IProjectionStore _projectionStore;
    private readonly PatientTrajectoryProjectionWriter _trajectoryProjectionWriter;
    private readonly ILogger<LocalOutboxMessageDispatcher> _logger;

    public LocalOutboxMessageDispatcher(
        IProjectionStore projectionStore,
        PatientTrajectoryProjectionWriter trajectoryProjectionWriter,
        ILogger<LocalOutboxMessageDispatcher> logger)
    {
        _projectionStore = projectionStore;
        _trajectoryProjectionWriter = trajectoryProjectionWriter;
        _logger = logger;
    }

    public async Task DispatchAsync(object eventPayload, Type eventType, CancellationToken cancellationToken)
    {
        switch (eventPayload)
        {
            case PatientCheckedIn ev:
                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "PatientName", ev.PatientName },
                    { "Status", "Waiting" },
                    { "TicketNumber", ev.PatientId }
                }, cancellationToken);
                break;

            case PatientCalled ev:
                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "Status", "Called" },
                    { "RoomAssigned", ev.RoomId }
                }, cancellationToken);
                break;

            case PatientClaimedForAttention ev:
                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "Status", "InConsultation" },
                    { "RoomAssigned", ev.RoomId }
                }, cancellationToken);
                break;

            case PatientAttentionCompleted ev:
                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "Status", "Completed" }
                }, cancellationToken);
                break;

            case PatientAbsentAtConsultation ev:
                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "Status", "Absent" }
                }, cancellationToken);
                break;

            case PatientTrajectoryOpened ev:
                await _trajectoryProjectionWriter.RefreshAsync(ev.AggregateId, cancellationToken);
                break;

            case PatientTrajectoryStageRecorded ev:
                await _trajectoryProjectionWriter.RefreshAsync(ev.AggregateId, cancellationToken);
                break;

            case PatientTrajectoryCompleted ev:
                await _trajectoryProjectionWriter.RefreshAsync(ev.AggregateId, cancellationToken);
                break;

            case PatientTrajectoryCancelled ev:
                await _trajectoryProjectionWriter.RefreshAsync(ev.AggregateId, cancellationToken);
                break;

            case PatientTrajectoryRebuilt ev:
                await _trajectoryProjectionWriter.RefreshAsync(ev.AggregateId, cancellationToken);
                break;

            default:
                _logger.LogDebug(
                    "No local outbox dispatcher handler registered for event type {EventType}. Skipping local dispatch.",
                    eventType.Name);
                break;
        }
    }
}
