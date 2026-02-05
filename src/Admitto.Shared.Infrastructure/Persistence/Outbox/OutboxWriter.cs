using System.Text.Json;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Contracts;
using Amolenk.Admitto.Shared.Kernel.DomainEvents;
using Humanizer;

namespace Amolenk.Admitto.Shared.Infrastructure.Persistence.Outbox;

internal class OutboxWriter(IOutboxDbContext dbContext, IMessagePolicy policy)
{
    public bool TryEnqueue(IDomainEvent domainEvent)
    {
        var enqueued = false;
        
        if (policy.ShouldPublishAsyncDomainEvent(domainEvent))
        {
            var messageType = GetMessageType(domainEvent);
            PersistMessageInOutbox(messageType, domainEvent);
            
            enqueued = true;
        }

        if (policy.ShouldPublishIntegrationEvent(domainEvent))
        {
            var integrationEvent = policy.MapToIntegrationEvent(domainEvent);
            var messageType = GetMessageType(integrationEvent);
            PersistMessageInOutbox(messageType, integrationEvent);
            
            enqueued = true;
        }

        return enqueued;
    }
    
    private void PersistMessageInOutbox(string messageType, object payload)
    {
        var serializedPayload = JsonSerializer.SerializeToDocument(payload, JsonSerializerOptions.Web);
        var message = OutboxMessage.Pending(messageType, serializedPayload);

        dbContext.OutboxMessages.Add(message);
    }

    // TODO Move to separate utility class
    private static string GetMessageType(IDomainEvent domainEvent)
    {
        var type = domainEvent.GetType();

        var ns = type.Namespace
                 ?? throw new InvalidOperationException(
                     $"Domain event {type.Name} has no namespace.");

        // Expected: Amolenk.Admitto.<ModuleName>.Domain.DomainEvents
        var parts = ns.Split('.');

        // Defensive validation so mistakes fail fast
        if (parts.Length < 5 ||
            parts[0] != "Amolenk" ||
            parts[1] != "Admitto" ||
            parts[^2] != "Domain" ||
            parts[^1] != "DomainEvents")
        {
            throw new InvalidOperationException(
                $"Domain event {type.FullName} does not follow the expected namespace convention.");
        }

        var moduleName = parts[2];
        var eventName = type.Name;

        return $"{moduleName.Kebaberize()}.domain.{eventName.Kebaberize()}";
    }
    
    // TODO Move to separate utility class
    private static string GetMessageType(IIntegrationEvent integrationEvent)
    {
        var type = integrationEvent.GetType();

        var ns = type.Namespace
                 ?? throw new InvalidOperationException(
                     $"Domain event {type.Name} has no namespace.");

        // Expected: Amolenk.Admitto.<ModuleName>.Contracts.IntegrationEvents
        var parts = ns.Split('.');

        // Defensive validation so mistakes fail fast
        if (parts.Length < 5 ||
            parts[0] != "Amolenk" ||
            parts[1] != "Admitto" ||
            parts[^2] != "Contracts" ||
            parts[^1] != "IntegrationEvents")
        {
            throw new InvalidOperationException(
                $"Integration event {type.FullName} does not follow the expected namespace convention.");
        }

        var moduleName = parts[2];
        var eventName = type.Name;

        return $"{moduleName.Kebaberize()}.integration.{eventName.Kebaberize()}";
    }
}
