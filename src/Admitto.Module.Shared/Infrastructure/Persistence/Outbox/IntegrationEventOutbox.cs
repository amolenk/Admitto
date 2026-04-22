using System.Text.Json;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Humanizer;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Outbox;

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
            parts[2] != "Module" ||
            parts[^2] != "Contracts" ||
            parts[^1] != "IntegrationEvents")
        {
            throw new InvalidOperationException(
                $"Integration event {type.FullName} does not follow the expected namespace convention.");
        }

        var moduleName = parts[3];
        var eventName = type.Name;

        return $"integration.{moduleName.Kebaberize()}.{eventName.Kebaberize()}";
    }
}
