namespace RLApp.Application.Queries;

using MediatR;
using RLApp.Application.DTOs;
using RLApp.Application.Handlers;

/// <summary>
/// Base query for all application queries.
/// </summary>
public abstract class Query
{
    public string CorrelationId { get; set; }

    protected Query(string correlationId)
    {
        CorrelationId = correlationId;
    }
}

/// <summary>
/// UC-006: View Queue Monitor
/// Query to get current queue status.
/// Reference: S-003 Queue Open and Check-in
/// </summary>
public class GetQueueMonitorQuery : Query
{
    public string QueueId { get; set; }

    public GetQueueMonitorQuery(string queueId, string correlationId)
        : base(correlationId)
    {
        QueueId = queueId;
    }
}

/// <summary>
/// UC-015: View Operations Dashboard
/// Query to get operations dashboard.
/// Reference: S-007 Reporting and Audit
/// </summary>
public class GetOperationsDashboardQuery : Query
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public GetOperationsDashboardQuery(DateTime fromDate, DateTime toDate, string correlationId)
        : base(correlationId)
    {
        FromDate = fromDate;
        ToDate = toDate;
    }
}

/// <summary>
/// UC-018: Reconstruct Patient Trajectory
/// Query to retrieve a persisted patient trajectory projection.
/// Reference: S-011 Patient Trajectory Aggregate
/// </summary>
public class GetPatientTrajectoryQuery : IRequest<QueryResult<PatientTrajectoryDto>>
{
    public string TrajectoryId { get; }
    public string CorrelationId { get; }

    public GetPatientTrajectoryQuery(string trajectoryId, string correlationId)
    {
        TrajectoryId = trajectoryId;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// UC-018: Reconstruct Patient Trajectory
/// Query to discover persisted patient trajectory candidates by patient and optional queue.
/// Reference: S-011 Patient Trajectory Aggregate
/// </summary>
public sealed class DiscoverPatientTrajectoriesQuery : IRequest<QueryResult<PatientTrajectoryDiscoveryResponseDto>>
{
    public string PatientId { get; }
    public string? QueueId { get; }
    public string CorrelationId { get; }

    public DiscoverPatientTrajectoriesQuery(string patientId, string? queueId, string correlationId)
    {
        PatientId = patientId;
        QueueId = queueId;
        CorrelationId = correlationId;
    }
}

public sealed class GetWaitingRoomMonitorSnapshotQuery : IRequest<QueryResult<WaitingRoomMonitorDto>>
{
    public string QueueId { get; }
    public string CorrelationId { get; }

    public GetWaitingRoomMonitorSnapshotQuery(string queueId, string correlationId)
    {
        QueueId = queueId;
        CorrelationId = correlationId;
    }
}

public sealed class GetOperationalDashboardSnapshotQuery : IRequest<QueryResult<OperationsDashboardSnapshotDto>>
{
    public string CorrelationId { get; }

    public GetOperationalDashboardSnapshotQuery(string correlationId)
    {
        CorrelationId = correlationId;
    }
}
