namespace RLApp.Infrastructure.BackgroundServices;

public sealed class OutboxProcessorOptions
{
    public const string SectionName = "OutboxProcessor";

    public int PollingIntervalMs { get; init; } = 500;

    public int BatchSize { get; init; } = 50;

    public TimeSpan PollingInterval => TimeSpan.FromMilliseconds(PollingIntervalMs);
}
