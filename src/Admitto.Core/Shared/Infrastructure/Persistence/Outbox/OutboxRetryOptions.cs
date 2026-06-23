namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

internal sealed class OutboxRetryOptions
{
    public const string SectionName = "OutboxRetry";

    public int BatchSize { get; init; } = 50;
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan MinimumAge { get; init; } = TimeSpan.FromSeconds(5);
}
