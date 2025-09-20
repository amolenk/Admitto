using Polly;

namespace Amolenk.Admitto.Worker;

public class MessageQueuesWorkerOptions
{
    public const string SectionName = nameof(MessageQueuesWorker);

    public required DelayBackoffType RetryBackoffType { get; init; } = DelayBackoffType.Exponential;

    public required TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(30);

    public required int MaxRetryAttempts { get; init; } = int.MaxValue;

    public required TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(5);
}