namespace RLApp.Ports.Outbound;

public static class OperationalVisibleStatuses
{
    public const string Unknown = "Unknown";
    public const string Waiting = "Waiting";
    public const string AtCashier = "AtCashier";
    public const string PaymentPending = "PaymentPending";
    public const string WaitingForConsultation = "WaitingForConsultation";
    public const string Called = "Called";
    public const string InConsultation = "InConsultation";
    public const string Completed = "Completed";
    public const string Absent = "Absent";

    private static readonly HashSet<string> WaitingBuckets = new(StringComparer.OrdinalIgnoreCase)
    {
        Waiting,
        WaitingForConsultation,
    };

    private static readonly HashSet<string> ActiveConsultationBuckets = new(StringComparer.OrdinalIgnoreCase)
    {
        InConsultation,
    };

    private static readonly HashSet<string> TerminalBuckets = new(StringComparer.OrdinalIgnoreCase)
    {
        Completed,
        Absent,
    };

    private static readonly Dictionary<string, int> ProgressRanks = new(StringComparer.OrdinalIgnoreCase)
    {
        [Unknown] = 0,
        [Waiting] = 10,
        [AtCashier] = 20,
        [PaymentPending] = 25,
        [WaitingForConsultation] = 30,
        [Called] = 40,
        [InConsultation] = 50,
        [Completed] = 60,
        [Absent] = 60,
    };

    public static bool CountsAsWaiting(string? status)
        => WaitingBuckets.Contains(Normalize(status));

    public static bool CountsAsActiveConsultation(string? status)
        => ActiveConsultationBuckets.Contains(Normalize(status));

    public static bool IsTerminal(string? status)
        => TerminalBuckets.Contains(Normalize(status));

    public static string Normalize(string? status)
        => string.IsNullOrWhiteSpace(status) ? Unknown : status.Trim();

    public static string ResolveNextStatus(string? currentStatus, string? requestedStatus)
    {
        var normalizedCurrent = Normalize(currentStatus);
        var normalizedRequested = Normalize(requestedStatus);

        if (normalizedCurrent == Unknown)
        {
            return normalizedRequested;
        }

        if (normalizedRequested == Unknown || string.Equals(normalizedCurrent, normalizedRequested, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedCurrent;
        }

        if (IsTerminal(normalizedCurrent))
        {
            return normalizedCurrent;
        }

        return GetRank(normalizedRequested) >= GetRank(normalizedCurrent)
            ? normalizedRequested
            : normalizedCurrent;
    }

    private static int GetRank(string status)
        => ProgressRanks.TryGetValue(status, out var rank) ? rank : ProgressRanks[Unknown];
}
