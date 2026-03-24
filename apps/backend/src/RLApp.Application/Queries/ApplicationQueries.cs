namespace RLApp.Application.Queries;

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
