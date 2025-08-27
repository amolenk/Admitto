namespace Amolenk.Admitto.Infrastructure.Messaging;

/// <summary>
/// Tracks processed messages to ensure idempotency.
/// </summary>
public record MessageLog(Guid Id, Guid MessageId, string HandlerType, DateTimeOffset ProcessedAt);
