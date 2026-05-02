namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Inbox;

public class ProcessedMessage
{
    public required Guid Id { get; init; }
    public required string MessageKey { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }

    public static ProcessedMessage Create(string messageKey, DateTimeOffset processedAt)
    {
        return new ProcessedMessage
        {
            Id = Guid.NewGuid(),
            MessageKey = messageKey,
            ProcessedAt = processedAt
        };
    }
}
