// using Polly;
//
// namespace Amolenk.Admitto.Worker;
//
// public class MessageOutboxWorkerOptions
// {
//     public const string SectionName = nameof(MessageOutboxWorker);
//
//     public required DelayBackoffType RetryBackoffType { get; init; } = DelayBackoffType.Exponential;
//
//     public required TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(30);
//
//     public required int MaxRetryAttempts { get; init; } = int.MaxValue;
// }