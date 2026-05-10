using System.Text.Json;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Humanizer;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

public sealed class Outbox(IOutboxDbContext dbContext) : IOutbox
{
    public void Enqueue(ICommand command)
    {
        var messageType = GetCommandMessageType(command);
        var payload = JsonSerializer.SerializeToDocument(
            command,
            command.GetType(),
            JsonSerializerOptions.Web);

        dbContext.OutboxMessages.Add(OutboxMessage.Pending(messageType, payload));
    }

    public void Enqueue(IIntegrationEvent integrationEvent)
    {
        var messageType = GetIntegrationEventMessageType(integrationEvent);
        var payload = JsonSerializer.SerializeToDocument(
            integrationEvent,
            integrationEvent.GetType(),
            JsonSerializerOptions.Web);

        dbContext.OutboxMessages.Add(OutboxMessage.Pending(messageType, payload));
    }

    private static string GetCommandMessageType(ICommand command)
    {
        var type = command.GetType();

        var ns = type.Namespace
                 ?? throw new InvalidOperationException(
                     $"Command {type.Name} has no namespace.");

        // Expected: Amolenk.Admitto.Core.<ModuleName>.Application.*
        var parts = ns.Split('.');

        if (parts.Length < 5 ||
            parts[0] != "Amolenk" ||
            parts[1] != "Admitto" ||
            parts[2] != "Core" ||
            parts[4] != "Application")
        {
            throw new InvalidOperationException(
                $"Command {type.FullName} does not follow the expected namespace convention " +
                $"(Amolenk.Admitto.Core.<Module>.Application.*).");
        }

        var moduleName = parts[3];
        var commandName = type.Name;
        const string suffix = "Command";
        if (commandName.EndsWith(suffix, StringComparison.Ordinal))
            commandName = commandName[..^suffix.Length];

        return $"command.{moduleName.Kebaberize()}.{commandName.Kebaberize()}";
    }

    private static string GetIntegrationEventMessageType(IIntegrationEvent integrationEvent)
    {
        var type = integrationEvent.GetType();

        var ns = type.Namespace
                 ?? throw new InvalidOperationException(
                     $"Integration event {type.Name} has no namespace.");

        // Expected: Amolenk.Admitto.Core.<ModuleName>.Contracts.IntegrationEvents
        var parts = ns.Split('.');

        if (parts.Length < 6 ||
            parts[0] != "Amolenk" ||
            parts[1] != "Admitto" ||
            parts[2] != "Core" ||
            parts[^2] != "Contracts" ||
            parts[^1] != "IntegrationEvents")
        {
            throw new InvalidOperationException(
                $"Integration event {type.FullName} does not follow the expected namespace convention.");
        }

        var moduleName = parts[3];
        var eventName = type.Name;
        const string suffix = "IntegrationEvent";
        if (eventName.EndsWith(suffix, StringComparison.Ordinal))
            eventName = eventName[..^suffix.Length];

        return $"integration.{moduleName.Kebaberize()}.{eventName.Kebaberize()}";
    }
}
