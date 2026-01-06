namespace Amolenk.Admitto.Infrastructure.Messaging;

/// <summary>
/// Tracks processed messages to ensure idempotency.
/// </summary>
public class MessageLog
{
    public required Guid Id { get; init; }
    
    public required Guid MessageId { get; init; }

    public required string MessageType { get; init; }

    public required string HandlerType { get; init; }

    public required DateTimeOffset ProcessedAt { get; init; }
}
