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
