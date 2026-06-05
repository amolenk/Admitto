using System.Text.Json;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

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

    public static OutboxMessage From(ICommand command) => Create(command);

    public static OutboxMessage From(IIntegrationEvent integrationEvent) => Create(integrationEvent);

    private static OutboxMessage Create(object message)
    {
        var type = GetMessageType(message);
        var payload = JsonSerializer.SerializeToDocument(
            message,
            message.GetType(),
            JsonSerializerOptions.Web);

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            State = OutboxMessageState.Pending
        };
    }

    private static string GetMessageType(object message)
    {
        var typeName = message.GetType().FullName!;
        var parts = typeName.Split('.');

        if (message is ICommand)
        {
            // Expected: Amolenk.Admitto.Core.<ModuleName>.Application.*
            if (parts.Length < 6 ||
                parts[0] != "Amolenk" ||
                parts[1] != "Admitto" ||
                parts[2] != "Core" ||
                parts[4] != "Application" ||
                parts[5] != "UseCases")
            {
                throw new InvalidOperationException(
                    $"Command {typeName} does not follow the expected namespace convention " +
                    $"(Amolenk.Admitto.Core.<Module>.Application.UseCases.*).");
            }
        }
        else
        {
            // Expected: Amolenk.Admitto.Core.<ModuleName>.Contracts.IntegrationEvents
            if (parts.Length < 6 ||
                parts[0] != "Amolenk" ||
                parts[1] != "Admitto" ||
                parts[2] != "Core" ||
                parts[4] != "Contracts" ||
                parts[5] != "IntegrationEvents")
            {
                throw new InvalidOperationException(
                    $"Integration event {typeName} does not follow the expected namespace convention.");
            }
        }

        return $"{parts[3]}:{string.Join('.', parts[6..])}";
    }
}
