using System.Text.Json;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Humanizer;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Outbox;

internal class OutboxWriter(IOutboxDbContext dbContext, IMessagePolicy messagePolicy)
{
    public bool TryEnqueue(IDomainEvent domainEvent)
    {
        var enqueued = false;
        
        if (messagePolicy.ShouldPublishModuleEvent(domainEvent))
        {
            var moduleEvent = messagePolicy.MapToModuleEvent(domainEvent);
            var messageType = GetMessageType(moduleEvent);
            PersistMessageInOutbox(messageType, moduleEvent);
            
            enqueued = true;
        }

        if (messagePolicy.ShouldPublishIntegrationEvent(domainEvent))
        {
            var integrationEvent = messagePolicy.MapToIntegrationEvent(domainEvent);
            var messageType = GetMessageType(integrationEvent);
            PersistMessageInOutbox(messageType, integrationEvent);
            
            enqueued = true;
        }

        return enqueued;
    }
    
    private void PersistMessageInOutbox(string messageType, object payload)
    {
        // TODO Isn't JsonDocument disposable? Do we need to dispose it after the message is published?
        var serializedPayload = JsonSerializer.SerializeToDocument(payload, JsonSerializerOptions.Web);
        var message = OutboxMessage.Pending(messageType, serializedPayload);

        dbContext.OutboxMessages.Add(message);
    }

    // TODO Move to separate utility class
    private static string GetMessageType(IModuleEvent moduleEvent)
    {
        var type = moduleEvent.GetType();

        var ns = type.Namespace
                 ?? throw new InvalidOperationException(
                     $"Domain event {type.Name} has no namespace.");

        // Expected: Amolenk.Admitto.Module.<ModuleName>.Application.ModuleEvents
        var parts = ns.Split('.');

        // Defensive validation so mistakes fail fast
        if (parts.Length < 6 ||
            parts[0] != "Amolenk" ||
            parts[1] != "Admitto" ||
            parts[2] != "Module" ||
            parts[^2] != "Application" ||
            parts[^1] != "ModuleEvents")
        {
            throw new InvalidOperationException(
                $"Module event {type.FullName} does not follow the expected namespace convention.");
        }

        var moduleName = parts[3];
        var eventName = type.Name;

        return $"{moduleName.Kebaberize()}.{eventName.Kebaberize()}";
    }
    
    // TODO Move to separate utility class
    private static string GetMessageType(IIntegrationEvent integrationEvent)
    {
        var type = integrationEvent.GetType();

        var ns = type.Namespace
                 ?? throw new InvalidOperationException(
                     $"Domain event {type.Name} has no namespace.");

        // Expected: Amolenk.Admitto.Module.<ModuleName>.Contracts.IntegrationEvents
        var parts = ns.Split('.');

        // Defensive validation so mistakes fail fast
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
