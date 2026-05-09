using System.Text.Json;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Humanizer;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

public sealed class IntegrationEventOutbox(IOutboxDbContext dbContext) : IIntegrationEventOutbox
{
    public void Enqueue(IIntegrationEvent integrationEvent)
    {
        var messageType = GetMessageType(integrationEvent);
        var payload = JsonSerializer.SerializeToDocument(
            integrationEvent,
            integrationEvent.GetType(),
            JsonSerializerOptions.Web);

        dbContext.OutboxMessages.Add(OutboxMessage.Pending(messageType, payload));
    }

    private static string GetMessageType(IIntegrationEvent integrationEvent)
    {
        var type = integrationEvent.GetType();

        var ns = type.Namespace
                 ?? throw new InvalidOperationException(
                     $"Integration event {type.Name} has no namespace.");

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
