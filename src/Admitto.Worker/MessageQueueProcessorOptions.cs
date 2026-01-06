using Polly;

namespace Amolenk.Admitto.Worker;

public class MessageQueueProcessorOptions
{
    public const string SectionName = nameof(MessageQueueProcessor);

    public required DelayBackoffType RetryBackoffType { get; init; } = DelayBackoffType.Exponential;

    public required TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(30);

    public required int MaxRetryAttempts { get; init; } = int.MaxValue;

    public required TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(5);
}