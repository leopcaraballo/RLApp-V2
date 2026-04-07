using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RLApp.Domain.Common;
using RLApp.Domain.Events;

namespace RLApp.Adapters.Messaging.Observability;

public static class MessageFlowTelemetry
{
    public const string ActivitySourceName = "RLApp.Adapters.Messaging";
    public const string SystemRole = "system";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static IReadOnlyDictionary<string, object?> CreateContext(
        DomainEvent message,
        string result,
        string? messageName = null,
        string? role = SystemRole,
        string? sagaName = null,
        string? consumerName = null,
        string? currentState = null,
        string? nextState = null)
    {
        var context = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["correlationId"] = message.CorrelationId,
            ["trajectoryId"] = ResolveTrajectoryId(message),
            ["queueId"] = ResolveQueueId(message),
            ["turnId"] = ResolveTurnId(message),
            ["role"] = role ?? SystemRole,
            ["result"] = result,
            ["messageName"] = messageName ?? message.EventType
        };

        if (!string.IsNullOrWhiteSpace(sagaName))
        {
            context["sagaName"] = sagaName;
        }

        if (!string.IsNullOrWhiteSpace(consumerName))
        {
            context["consumerName"] = consumerName;
        }

        if (!string.IsNullOrWhiteSpace(currentState))
        {
            context["currentState"] = currentState;
        }

        if (!string.IsNullOrWhiteSpace(nextState))
        {
            context["nextState"] = nextState;
        }

        return context;
    }

    public static IDisposable BeginScope(
        ILogger logger,
        DomainEvent message,
        string result,
        string? messageName = null,
        string? role = SystemRole,
        string? sagaName = null,
        string? consumerName = null,
        string? currentState = null,
        string? nextState = null)
    {
        return logger.BeginScope(CreateContext(message, result, messageName, role, sagaName, consumerName, currentState, nextState));
    }

    public static Activity? StartConsumerActivity(
        DomainEvent message,
        string consumerName,
        string result = "projection-pending")
    {
        return StartActivity(
            $"{consumerName}.{message.EventType}",
            ActivityKind.Consumer,
            message,
            result,
            consumerName: consumerName);
    }

    public static Activity? StartSagaActivity(
        DomainEvent message,
        string sagaName,
        string currentState,
        string nextState,
        string result = "transition-pending")
    {
        return StartActivity(
            $"{sagaName}.{message.EventType}",
            ActivityKind.Internal,
            message,
            result,
            sagaName: sagaName,
            currentState: currentState,
            nextState: nextState);
    }

    public static void SetResult(Activity? activity, string result)
    {
        activity?.SetTag("result", result);
    }

    public static void RecordFailure(Activity? activity, Exception exception, string result)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag("result", result);
        activity.SetTag("error.type", exception.GetType().Name);
    }

    public static string NormalizeState(string? state)
    {
        return string.IsNullOrWhiteSpace(state) ? "Initial" : state;
    }

    private static Activity? StartActivity(
        string operationName,
        ActivityKind kind,
        DomainEvent message,
        string result,
        string? sagaName = null,
        string? consumerName = null,
        string? currentState = null,
        string? nextState = null)
    {
        var activity = ActivitySource.StartActivity(operationName, kind);
        if (activity is null)
        {
            return null;
        }

        foreach (var entry in CreateContext(message, result, sagaName: sagaName, consumerName: consumerName, currentState: currentState, nextState: nextState))
        {
            if (entry.Value is not null)
            {
                activity.SetTag(entry.Key, entry.Value);
            }
        }

        return activity;
    }

    private static string? ResolveTrajectoryId(DomainEvent message)
    {
        return message switch
        {
            PatientTrajectoryOpened => message.AggregateId,
            PatientTrajectoryStageRecorded => message.AggregateId,
            PatientTrajectoryCompleted => message.AggregateId,
            PatientTrajectoryCancelled => message.AggregateId,
            PatientTrajectoryRebuilt => message.AggregateId,
            _ => message.TrajectoryId
        };
    }

    private static string? ResolveQueueId(DomainEvent message)
    {
        return message switch
        {
            WaitingQueueCreated => message.AggregateId,
            PatientCheckedIn => message.AggregateId,
            PatientCalledAtCashier => message.AggregateId,
            PatientPaymentValidated => message.AggregateId,
            PatientPaymentPending => message.AggregateId,
            PatientAbsentAtCashier => message.AggregateId,
            PatientClaimedForAttention => message.AggregateId,
            PatientCalled => message.AggregateId,
            PatientAttentionCompleted => message.AggregateId,
            PatientAbsentAtConsultation => message.AggregateId,
            PatientTrajectoryOpened e => e.QueueId,
            PatientTrajectoryStageRecorded e => e.QueueId,
            PatientTrajectoryCompleted e => e.QueueId,
            PatientTrajectoryCancelled e => e.QueueId,
            PatientTrajectoryRebuilt e => e.QueueId,
            _ => null
        };
    }

    private static string? ResolveTurnId(DomainEvent message)
    {
        return message switch
        {
            PatientPaymentValidated e => e.TurnId,
            PatientAbsentAtCashier e => e.TurnId,
            PatientAttentionCompleted e => e.TurnId,
            PatientAbsentAtConsultation e => e.TurnId,
            _ => null
        };
    }
}
