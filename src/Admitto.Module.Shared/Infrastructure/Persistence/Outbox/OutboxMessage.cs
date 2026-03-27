using System.Text.Json;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Outbox;

public enum OutboxMessageState
{
    Pending,
    Sent
}

public class OutboxMessage
{
    public required Guid Id { get; init; }
    public required string Type { get; init; }
    public required JsonDocument Payload { get; init; }
    public required OutboxMessageState State { get; set; }

    public static OutboxMessage Pending(string type, JsonDocument payload)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            State = OutboxMessageState.Pending
        };
    }
}