using RLApp.Infrastructure.Observability;

namespace RLApp.Infrastructure.Realtime;

public sealed class RealtimeChannelStatus
{
    private readonly object _gate = new();
    private int _activeConnections;
    private DateTimeOffset? _lastPublishedAt;
    private DateTimeOffset? _lastPublishSucceededAt;
    private DateTimeOffset? _lastPublishFailedAt;
    private int _consecutivePublishFailures;
    private string? _lastEventType;
    private string? _lastDeliveryScope;
    private string? _lastFailureType;

    public void RecordConnectionOpened()
    {
        int activeConnections;

        lock (_gate)
        {
            _activeConnections++;
            activeConnections = _activeConnections;
        }

        RealtimeChannelTelemetry.RecordConnectionOpened(activeConnections);
    }

    public void RecordConnectionClosed()
    {
        int activeConnections;

        lock (_gate)
        {
            _activeConnections = Math.Max(0, _activeConnections - 1);
            activeConnections = _activeConnections;
        }

        RealtimeChannelTelemetry.RecordConnectionClosed(activeConnections);
    }

    public void RecordPublishSucceeded(string eventType, string deliveryScope, TimeSpan duration)
    {
        var recordedAt = DateTimeOffset.UtcNow;
        int activeConnections;

        lock (_gate)
        {
            _lastPublishedAt = recordedAt;
            _lastPublishSucceededAt = recordedAt;
            _lastEventType = eventType;
            _lastDeliveryScope = deliveryScope;
            _lastFailureType = null;
            _consecutivePublishFailures = 0;
            activeConnections = _activeConnections;
        }

        RealtimeChannelTelemetry.RecordPublished(eventType, deliveryScope, duration, activeConnections);
    }

    public void RecordPublishFailed(string eventType, string deliveryScope, TimeSpan duration, Exception exception)
    {
        var recordedAt = DateTimeOffset.UtcNow;
        int activeConnections;

        lock (_gate)
        {
            _lastPublishedAt = recordedAt;
            _lastPublishFailedAt = recordedAt;
            _lastEventType = eventType;
            _lastDeliveryScope = deliveryScope;
            _lastFailureType = exception.GetType().Name;
            _consecutivePublishFailures++;
            activeConnections = _activeConnections;
        }

        RealtimeChannelTelemetry.RecordPublishFailed(eventType, deliveryScope, exception.GetType().Name, duration, activeConnections);
    }

    public RealtimeChannelSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            return new RealtimeChannelSnapshot(
                _activeConnections,
                _lastPublishedAt,
                _lastPublishSucceededAt,
                _lastPublishFailedAt,
                _consecutivePublishFailures,
                _lastEventType,
                _lastDeliveryScope,
                _lastFailureType);
        }
    }
}

public readonly record struct RealtimeChannelSnapshot(
    int ActiveConnections,
    DateTimeOffset? LastPublishedAt,
    DateTimeOffset? LastPublishSucceededAt,
    DateTimeOffset? LastPublishFailedAt,
    int ConsecutivePublishFailures,
    string? LastEventType,
    string? LastDeliveryScope,
    string? LastFailureType);
