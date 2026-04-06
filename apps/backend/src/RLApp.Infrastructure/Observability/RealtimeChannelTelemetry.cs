using System.Diagnostics.Metrics;

namespace RLApp.Infrastructure.Observability;

public static class RealtimeChannelTelemetry
{
    public const string MeterName = "RLApp.Infrastructure.Realtime";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> ConnectionsOpened = Meter.CreateCounter<long>("rlapp.realtime.connections.opened");
    private static readonly Counter<long> ConnectionsClosed = Meter.CreateCounter<long>("rlapp.realtime.connections.closed");
    private static readonly Counter<long> Publications = Meter.CreateCounter<long>("rlapp.realtime.publications");
    private static readonly Counter<long> PublicationFailures = Meter.CreateCounter<long>("rlapp.realtime.publication.failures");
    private static readonly Histogram<double> PublicationDurationMs = Meter.CreateHistogram<double>("rlapp.realtime.publication.duration.ms");
    private static readonly Histogram<long> ActiveConnections = Meter.CreateHistogram<long>("rlapp.realtime.connections.active");

    public static void RecordConnectionOpened(int activeConnections)
    {
        ConnectionsOpened.Add(1);
        ActiveConnections.Record(activeConnections, new KeyValuePair<string, object?>("transition", "opened"));
    }

    public static void RecordConnectionClosed(int activeConnections)
    {
        ConnectionsClosed.Add(1);
        ActiveConnections.Record(activeConnections, new KeyValuePair<string, object?>("transition", "closed"));
    }

    public static void RecordPublished(string eventType, string deliveryScope, TimeSpan duration, int activeConnections)
    {
        var tags = CreateTags(eventType, deliveryScope, activeConnections, "success");
        Publications.Add(1, tags);
        PublicationDurationMs.Record(duration.TotalMilliseconds, tags);
        ActiveConnections.Record(activeConnections, tags);
    }

    public static void RecordPublishFailed(string eventType, string deliveryScope, string errorType, TimeSpan duration, int activeConnections)
    {
        var tags = CreateTags(eventType, deliveryScope, activeConnections, "failure", errorType);
        PublicationFailures.Add(1, tags);
        PublicationDurationMs.Record(duration.TotalMilliseconds, tags);
        ActiveConnections.Record(activeConnections, tags);
    }

    private static KeyValuePair<string, object?>[] CreateTags(
        string eventType,
        string deliveryScope,
        int activeConnections,
        string result,
        string? errorType = null)
    {
        var tags = new List<KeyValuePair<string, object?>>()
        {
            new("event.type", eventType),
            new("delivery.scope", deliveryScope),
            new("result", result),
            new("connection.state", activeConnections > 0 ? "active" : "idle")
        };

        if (!string.IsNullOrWhiteSpace(errorType))
        {
            tags.Add(new("error.type", errorType));
        }

        return tags.ToArray();
    }
}
