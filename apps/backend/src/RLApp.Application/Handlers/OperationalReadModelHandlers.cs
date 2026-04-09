namespace RLApp.Application.Handlers;

using MediatR;
using RLApp.Application.DTOs;
using RLApp.Application.Queries;
using RLApp.Ports.Outbound;

public sealed class GetWaitingRoomMonitorSnapshotHandler : IRequestHandler<GetWaitingRoomMonitorSnapshotQuery, QueryResult<WaitingRoomMonitorDto>>
{
    private readonly IProjectionStore _projectionStore;

    public GetWaitingRoomMonitorSnapshotHandler(IProjectionStore projectionStore)
    {
        _projectionStore = projectionStore;
    }

    public async Task<QueryResult<WaitingRoomMonitorDto>> Handle(GetWaitingRoomMonitorSnapshotQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.QueueId))
        {
            return QueryResult<WaitingRoomMonitorDto>.Failure("WAITING_ROOM_MONITOR_NOT_FOUND", query.CorrelationId);
        }

        var projection = await _projectionStore.GetWaitingRoomMonitorAsync(query.QueueId, cancellationToken);
        if (projection is null)
        {
            return QueryResult<WaitingRoomMonitorDto>.Failure("WAITING_ROOM_MONITOR_NOT_FOUND", query.CorrelationId);
        }

        return QueryResult<WaitingRoomMonitorDto>.Ok(
            new WaitingRoomMonitorDto
            {
                QueueId = projection.QueueId,
                GeneratedAt = projection.GeneratedAt,
                WaitingCount = projection.WaitingCount,
                AverageWaitTimeMinutes = projection.AverageWaitTimeMinutes,
                ActiveConsultationRooms = projection.ActiveConsultationRooms,
                StatusBreakdown = projection.StatusBreakdown
                    .Select(item => new OperationalStatusCountDto
                    {
                        Status = item.Status,
                        Total = item.Total
                    })
                    .ToArray(),
                Entries = projection.Entries
                    .Select(item => new WaitingRoomMonitorEntryDto
                    {
                        TurnId = item.TurnId,
                        PatientId = item.PatientId,
                        PatientName = item.PatientName,
                        TicketNumber = item.TicketNumber,
                        Status = item.Status,
                        RoomAssigned = item.RoomAssigned,
                        CheckedInAt = item.CheckedInAt,
                        UpdatedAt = item.UpdatedAt
                    })
                    .ToArray()
            },
            query.CorrelationId);
    }
}

public sealed class GetPublicWaitingRoomDisplayHandler : IRequestHandler<GetPublicWaitingRoomDisplayQuery, QueryResult<PublicWaitingRoomDisplayDto>>
{
    private readonly IProjectionStore _projectionStore;

    public GetPublicWaitingRoomDisplayHandler(IProjectionStore projectionStore)
    {
        _projectionStore = projectionStore;
    }

    public async Task<QueryResult<PublicWaitingRoomDisplayDto>> Handle(GetPublicWaitingRoomDisplayQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.QueueId))
        {
            return QueryResult<PublicWaitingRoomDisplayDto>.Failure("PUBLIC_WAITING_ROOM_DISPLAY_NOT_FOUND", query.CorrelationId);
        }

        var projection = await _projectionStore.GetWaitingRoomMonitorAsync(query.QueueId, cancellationToken);
        if (projection is null)
        {
            return QueryResult<PublicWaitingRoomDisplayDto>.Ok(
                new PublicWaitingRoomDisplayDto
                {
                    QueueId = query.QueueId,
                    GeneratedAt = DateTime.UtcNow,
                    CurrentTurn = null,
                    UpcomingTurns = Array.Empty<PublicWaitingRoomTurnDto>(),
                    ActiveCalls = Array.Empty<PublicWaitingRoomCallDto>()
                },
                query.CorrelationId);
        }

        var activeCallEntries = projection.Entries
            .Where(IsDisplayActiveTurn)
            .Where(HasVisibleDestination)
            .OrderByDescending(entry => entry.UpdatedAt)
            .ThenBy(entry => NormalizeDestination(entry.RoomAssigned), StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToArray();

        var upcomingTurns = projection.Entries
            .Where(IsUpcomingTurn)
            .OrderBy(entry => entry.CheckedInAt)
            .ThenBy(entry => ReadVisibleTurnNumber(entry), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var currentTurn = activeCallEntries.FirstOrDefault() ?? upcomingTurns.FirstOrDefault();

        return QueryResult<PublicWaitingRoomDisplayDto>.Ok(
            new PublicWaitingRoomDisplayDto
            {
                QueueId = projection.QueueId,
                GeneratedAt = projection.GeneratedAt,
                CurrentTurn = currentTurn is null ? null : MapTurn(currentTurn),
                UpcomingTurns = upcomingTurns
                    .Where(entry => currentTurn is null || !string.Equals(entry.TurnId, currentTurn.TurnId, StringComparison.OrdinalIgnoreCase))
                    .Take(12)
                    .Select(MapTurn)
                    .ToArray(),
                ActiveCalls = activeCallEntries
                    .Select(MapCall)
                    .ToArray()
            },
            query.CorrelationId);
    }

    private static bool IsDisplayActiveTurn(WaitingRoomMonitorEntryProjection entry)
        => string.Equals(entry.Status, OperationalVisibleStatuses.Called, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.Status, OperationalVisibleStatuses.AtCashier, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.Status, OperationalVisibleStatuses.InConsultation, StringComparison.OrdinalIgnoreCase);

    private static bool IsUpcomingTurn(WaitingRoomMonitorEntryProjection entry)
        => string.Equals(entry.Status, OperationalVisibleStatuses.Waiting, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.Status, OperationalVisibleStatuses.WaitingForConsultation, StringComparison.OrdinalIgnoreCase);

    private static PublicWaitingRoomTurnDto MapTurn(WaitingRoomMonitorEntryProjection entry)
        => new()
        {
            TurnNumber = ReadVisibleTurnNumber(entry)
        };

    private static PublicWaitingRoomCallDto MapCall(WaitingRoomMonitorEntryProjection entry)
        => new()
        {
            TurnNumber = ReadVisibleTurnNumber(entry),
            Destination = NormalizeDestination(entry.RoomAssigned)!,
            Status = entry.Status
        };

    private static bool HasVisibleDestination(WaitingRoomMonitorEntryProjection entry)
        => NormalizeDestination(entry.RoomAssigned) is not null;

    private static string ReadVisibleTurnNumber(WaitingRoomMonitorEntryProjection entry)
        => string.IsNullOrWhiteSpace(entry.TicketNumber) ? entry.TurnId : entry.TicketNumber;

    private static string? NormalizeDestination(string? destination)
        => string.IsNullOrWhiteSpace(destination) ? null : destination.Trim();
}

public sealed class GetOperationalDashboardSnapshotHandler : IRequestHandler<GetOperationalDashboardSnapshotQuery, QueryResult<OperationsDashboardSnapshotDto>>
{
    private readonly IProjectionStore _projectionStore;

    public GetOperationalDashboardSnapshotHandler(IProjectionStore projectionStore)
    {
        _projectionStore = projectionStore;
    }

    public async Task<QueryResult<OperationsDashboardSnapshotDto>> Handle(GetOperationalDashboardSnapshotQuery query, CancellationToken cancellationToken)
    {
        var projection = await _projectionStore.GetOperationsDashboardAsync(cancellationToken);

        return QueryResult<OperationsDashboardSnapshotDto>.Ok(
            new OperationsDashboardSnapshotDto
            {
                GeneratedAt = projection.GeneratedAt,
                CurrentWaitingCount = projection.CurrentWaitingCount,
                AverageWaitTimeMinutes = projection.AverageWaitTimeMinutes,
                TotalPatientsToday = projection.TotalPatientsToday,
                TotalCompleted = projection.TotalCompleted,
                ActiveRooms = projection.ActiveRooms,
                ProjectionLagSeconds = projection.ProjectionLagSeconds,
                QueueSnapshots = projection.QueueSnapshots
                    .Select(item => new DashboardQueueSnapshotDto
                    {
                        QueueId = item.QueueId,
                        TotalPending = item.TotalPending,
                        AverageWaitTimeMinutes = item.AverageWaitTimeMinutes,
                        LastUpdatedAt = item.LastUpdatedAt
                    })
                    .ToArray(),
                StatusBreakdown = projection.StatusBreakdown
                    .Select(item => new OperationalStatusCountDto
                    {
                        Status = item.Status,
                        Total = item.Total
                    })
                    .ToArray()
            },
            query.CorrelationId);
    }
}
