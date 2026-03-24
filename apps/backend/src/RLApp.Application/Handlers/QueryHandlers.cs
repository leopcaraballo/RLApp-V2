namespace RLApp.Application.Handlers;

using DTOs;
using Queries;
using RLApp.Ports.Inbound;

/// <summary>
/// DTO for queue monitor view.
/// Represents current state of the waiting queue.
/// </summary>
public class QueueMonitorDto
{
    public string QueueId { get; set; }
    public bool IsOpen { get; set; }
    public int TotalPatients { get; set; }
    public List<PatientInQueueDto> Patients { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// DTO for patient in queue.
/// </summary>
public class PatientInQueueDto
{
    public string PatientId { get; set; }
    public int Position { get; set; }
    public DateTime CheckInTime { get; set; }
    public string Status { get; set; }
}

/// <summary>
/// Handler for UC-006: View Queue Monitor
/// Retrieves current queue status and patient positions.
/// Reference: S-003 Queue Open and Check-in
/// </summary>
public class GetQueueMonitorHandler
{
    private readonly IWaitingQueueRepository _queueRepository;

    public GetQueueMonitorHandler(IWaitingQueueRepository queueRepository)
    {
        _queueRepository = queueRepository;
    }

    public async Task<QueryResult<QueueMonitorDto>> Handle(GetQueueMonitorQuery query)
    {
        try
        {
            var queue = await _queueRepository.GetByIdAsync(query.QueueId);

            if (queue == null)
                return QueryResult<QueueMonitorDto>.Failure("Queue not found", query.CorrelationId);

            var patients = queue.PatientIds.Select((patientId, index) => new PatientInQueueDto
            {
                PatientId = patientId,
                Position = index + 1,
                CheckInTime = DateTime.UtcNow, // TODO: Get actual check-in time from event store
                Status = "Waiting"
            }).ToList();

            var result = new QueueMonitorDto
            {
                QueueId = queue.Id,
                IsOpen = queue.IsOpen,
                TotalPatients = queue.GetQueueSize(),
                Patients = patients,
                LastUpdated = DateTime.UtcNow
            };

            return QueryResult<QueueMonitorDto>.Ok(result, query.CorrelationId);
        }
        catch (Exception ex)
        {
            return QueryResult<QueueMonitorDto>.Failure($"Query failed: {ex.Message}", query.CorrelationId);
        }
    }
}

/// <summary>
/// DTO for operations dashboard.
/// </summary>
public class OperationsDashboardDto
{
    public int TotalPatientsProcessed { get; set; }
    public int TotalPaymentsValidated { get; set; }
    public decimal TotalRevenueProcessed { get; set; }
    public int AverageWaitTime { get; set; }
    public int ActiveConsultingRooms { get; set; }
    public DateTime ReportFrom { get; set; }
    public DateTime ReportTo { get; set; }
}

/// <summary>
/// Handler for UC-015: View Operations Dashboard
/// Retrieves operations metrics and statistics.
/// Reference: S-007 Reporting and Audit
/// </summary>
public class GetOperationsDashboardHandler
{
    private readonly IWaitingQueueRepository _queueRepository;

    public GetOperationsDashboardHandler(IWaitingQueueRepository queueRepository)
    {
        _queueRepository = queueRepository;
    }

    public async Task<QueryResult<OperationsDashboardDto>> Handle(GetOperationsDashboardQuery query)
    {
        try
        {
            // TODO: Integrate with projection store for aggregated metrics
            var result = new OperationsDashboardDto
            {
                TotalPatientsProcessed = 0,
                TotalPaymentsValidated = 0,
                TotalRevenueProcessed = 0,
                AverageWaitTime = 0,
                ActiveConsultingRooms = 0,
                ReportFrom = query.FromDate,
                ReportTo = query.ToDate
            };

            return QueryResult<OperationsDashboardDto>.Ok(result, query.CorrelationId);
        }
        catch (Exception ex)
        {
            return QueryResult<OperationsDashboardDto>.Failure($"Query failed: {ex.Message}", query.CorrelationId);
        }
    }
}

/// <summary>
/// Result object for query execution.
/// </summary>
public class QueryResult<T> where T : class
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string CorrelationId { get; set; }
    public T Data { get; set; }
    public DateTime ExecutedAt { get; set; }

    public static QueryResult<T> Ok(T data, string correlationId, string message = "Query executed successfully")
        => new() { Success = true, Data = data, Message = message, CorrelationId = correlationId, ExecutedAt = DateTime.UtcNow };

    public static QueryResult<T> Failure(string message, string correlationId)
        => new() { Success = false, Message = message, CorrelationId = correlationId, ExecutedAt = DateTime.UtcNow };
}
