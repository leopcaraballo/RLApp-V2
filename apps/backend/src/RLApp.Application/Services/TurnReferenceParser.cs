namespace RLApp.Application.Services;

public static class TurnReferenceParser
{
    public static string Build(string queueId, string patientId) => $"{queueId}-{patientId}";

    public static bool TryExtractPatientId(string turnId, string queueId, out string patientId)
    {
        patientId = string.Empty;

        if (string.IsNullOrWhiteSpace(turnId) || string.IsNullOrWhiteSpace(queueId))
        {
            return false;
        }

        var prefix = $"{queueId}-";
        if (!turnId.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        patientId = turnId[prefix.Length..];
        return !string.IsNullOrWhiteSpace(patientId);
    }

    public static bool TryExtractQueueId(string turnId, string patientId, out string queueId)
    {
        queueId = string.Empty;

        if (string.IsNullOrWhiteSpace(turnId) || string.IsNullOrWhiteSpace(patientId))
        {
            return false;
        }

        var suffix = $"-{patientId}";
        if (!turnId.EndsWith(suffix, StringComparison.Ordinal))
        {
            return false;
        }

        queueId = turnId[..^suffix.Length];
        return !string.IsNullOrWhiteSpace(queueId);
    }
}
