using System.Diagnostics.Metrics;

namespace RLApp.Infrastructure.BackgroundServices;

public static class OutboxProcessorTelemetry
{
    public const string MeterName = "RLApp.Infrastructure.Outbox";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> PublishedMessages = Meter.CreateCounter<long>("rlapp.outbox.messages.published");
    private static readonly Counter<long> FailedMessages = Meter.CreateCounter<long>("rlapp.outbox.messages.failed");
    private static readonly Histogram<long> BacklogCount = Meter.CreateHistogram<long>("rlapp.outbox.backlog.count");
    private static readonly Histogram<double> PublishDurationMs = Meter.CreateHistogram<double>("rlapp.outbox.publish.duration.ms");
    private static readonly Histogram<double> PropagationDelayMs = Meter.CreateHistogram<double>("rlapp.outbox.propagation.delay.ms");

    public static void RecordBacklog(int backlogCount)
    {
        BacklogCount.Record(backlogCount);
    }

    public static void RecordPublished(string eventType, TimeSpan publishDuration, TimeSpan propagationDelay)
    {
        var eventTypeTag = new KeyValuePair<string, object?>("event.type", eventType);

        PublishedMessages.Add(1, eventTypeTag);
        PublishDurationMs.Record(publishDuration.TotalMilliseconds, eventTypeTag);
        PropagationDelayMs.Record(propagationDelay.TotalMilliseconds, eventTypeTag);
    }

    public static void RecordFailed(string eventType)
    {
        FailedMessages.Add(1, new KeyValuePair<string, object?>("event.type", eventType));
    }
}
