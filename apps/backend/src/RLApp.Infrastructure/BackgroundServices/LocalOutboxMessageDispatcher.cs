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
                var turnId = $"{ev.AggregateId}-{ev.PatientId}";
                await _projectionStore.UpsertAsync(turnId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "QueueId", ev.AggregateId },
                    { "PatientId", ev.PatientId },
                    { "TurnId", turnId },
                    { "PatientName", ev.PatientName },
                    { "TicketNumber", turnId },
                    { "CheckedInAt", ev.OccurredAt },
                    { "UpdatedAt", ev.OccurredAt },
                    { "Status", OperationalVisibleStatuses.Waiting },
                }, cancellationToken);
                break;

            case PatientCalledAtCashier ev:
                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "PatientId", ev.PatientId },
                    { "UpdatedAt", ev.OccurredAt },
                    { "Status", OperationalVisibleStatuses.AtCashier }
                }, cancellationToken);
                break;

            case PatientPaymentValidated ev:
                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "PatientId", ev.PatientId },
                    { "TurnId", ev.TurnId ?? $"{ev.AggregateId}-{ev.PatientId}" },
                    { "UpdatedAt", ev.OccurredAt },
                    { "Status", OperationalVisibleStatuses.WaitingForConsultation }
                }, cancellationToken);
                break;

            case PatientPaymentPending ev:
                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "PatientId", ev.PatientId },
                    { "UpdatedAt", ev.OccurredAt },
                    { "Status", OperationalVisibleStatuses.PaymentPending }
                }, cancellationToken);
                break;

            case PatientCalled ev:
                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "PatientId", ev.PatientId },
                    { "UpdatedAt", ev.OccurredAt },
                    { "Status", OperationalVisibleStatuses.Called },
                    { "RoomAssigned", ev.RoomId }
                }, cancellationToken);
                break;

            case PatientClaimedForAttention ev:
                if (!ev.RepresentsStartedAttention)
                {
                    break;
                }

                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "PatientId", ev.PatientId },
                    { "UpdatedAt", ev.OccurredAt },
                    { "Status", OperationalVisibleStatuses.InConsultation },
                    { "RoomAssigned", ev.RoomId }
                }, cancellationToken);
                break;

            case PatientAttentionCompleted ev:
                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "PatientId", ev.PatientId },
                    { "TurnId", ev.TurnId ?? $"{ev.AggregateId}-{ev.PatientId}" },
                    { "UpdatedAt", ev.OccurredAt },
                    { "Status", OperationalVisibleStatuses.Completed }
                }, cancellationToken);
                break;

            case PatientAbsentAtConsultation ev:
                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "PatientId", ev.PatientId },
                    { "TurnId", ev.TurnId ?? $"{ev.AggregateId}-{ev.PatientId}" },
                    { "UpdatedAt", ev.OccurredAt },
                    { "Status", OperationalVisibleStatuses.Absent }
                }, cancellationToken);
                break;

            case PatientAbsentAtCashier ev:
                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "PatientId", ev.PatientId },
                    { "TurnId", ev.TurnId ?? $"{ev.AggregateId}-{ev.PatientId}" },
                    { "UpdatedAt", ev.OccurredAt },
                    { "Status", OperationalVisibleStatuses.Absent }
                }, cancellationToken);
                break;

            case PatientCancelledByPayment ev:
                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "PatientId", ev.PatientId },
                    { "UpdatedAt", ev.OccurredAt },
                    { "Status", OperationalVisibleStatuses.Cancelled }
                }, cancellationToken);
                break;

            case PatientCancelledByAbsence ev:
                await _projectionStore.UpsertAsync(ev.PatientId, "WaitingRoomMonitor", new Dictionary<string, object>
                {
                    { "PatientId", ev.PatientId },
                    { "UpdatedAt", ev.OccurredAt },
                    { "Status", OperationalVisibleStatuses.Cancelled }
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
