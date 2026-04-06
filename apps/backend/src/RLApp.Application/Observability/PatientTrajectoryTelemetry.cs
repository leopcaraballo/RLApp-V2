using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RLApp.Application.Observability;

public static class PatientTrajectoryTelemetry
{
    public const string ActivitySourceName = "RLApp.Application.PatientTrajectory";
    public const string MeterName = "RLApp.Application.PatientTrajectory";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> DiscoveryRequests = Meter.CreateCounter<long>("rlapp.patient_trajectory.discovery.requests");
    private static readonly Counter<long> DiscoveryFailures = Meter.CreateCounter<long>("rlapp.patient_trajectory.discovery.failures");
    private static readonly Histogram<double> DiscoveryDurationMs = Meter.CreateHistogram<double>("rlapp.patient_trajectory.discovery.duration.ms");
    private static readonly Histogram<long> DiscoveryMatchCount = Meter.CreateHistogram<long>("rlapp.patient_trajectory.discovery.match_count");

    public static Activity? StartDiscoveryActivity(string correlationId, string patientId, string? queueId)
    {
        var activity = ActivitySource.StartActivity("PatientTrajectory.Discover", ActivityKind.Internal);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag("correlationId", correlationId);
        activity.SetTag("patientId", patientId);

        if (!string.IsNullOrWhiteSpace(queueId))
        {
            activity.SetTag("queueId", queueId);
            activity.SetTag("queue.filter_applied", true);
        }
        else
        {
            activity.SetTag("queue.filter_applied", false);
        }

        return activity;
    }

    public static void SetDiscoveryResult(Activity? activity, string result, int matchCount)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("result", result);
        activity.SetTag("matchCount", matchCount);
    }

    public static void RecordDiscoveryCompleted(int matchCount, TimeSpan duration, bool queueFilterApplied)
    {
        var tags = CreateTags("success", queueFilterApplied);
        DiscoveryRequests.Add(1, tags);
        DiscoveryDurationMs.Record(duration.TotalMilliseconds, tags);
        DiscoveryMatchCount.Record(matchCount, tags);
    }

    public static void RecordDiscoveryRejected(TimeSpan duration, bool queueFilterApplied, string result)
    {
        var tags = CreateTags(result, queueFilterApplied);
        DiscoveryRequests.Add(1, tags);
        DiscoveryDurationMs.Record(duration.TotalMilliseconds, tags);
        DiscoveryMatchCount.Record(0, tags);
    }

    public static void RecordDiscoveryFailed(TimeSpan duration, bool queueFilterApplied, string errorType)
    {
        var tags = CreateTags("failure", queueFilterApplied, errorType);
        DiscoveryRequests.Add(1, tags);
        DiscoveryFailures.Add(1, tags);
        DiscoveryDurationMs.Record(duration.TotalMilliseconds, tags);
    }

    public static void RecordFailure(Activity? activity, Exception exception)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag("result", "failure");
        activity.SetTag("error.type", exception.GetType().Name);
    }

    private static KeyValuePair<string, object?>[] CreateTags(string result, bool queueFilterApplied, string? errorType = null)
    {
        var tags = new List<KeyValuePair<string, object?>>()
        {
            new("result", result),
            new("queue.filter_applied", queueFilterApplied)
        };

        if (!string.IsNullOrWhiteSpace(errorType))
        {
            tags.Add(new("error.type", errorType));
        }

        return tags.ToArray();
    }
}
