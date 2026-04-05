namespace RLApp.Domain.Common;

public static class PatientTrajectoryIdFactory
{
    public static string Create(string queueId, string patientId, DateTime occurredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueId);
        ArgumentException.ThrowIfNullOrWhiteSpace(patientId);

        return $"TRJ-{Normalize(queueId)}-{Normalize(patientId)}-{occurredAt:yyyyMMddHHmmssfff}";
    }

    private static string Normalize(string value)
    {
        var normalized = new string(value
            .Trim()
            .ToUpperInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray());

        return string.Join('-', normalized
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
