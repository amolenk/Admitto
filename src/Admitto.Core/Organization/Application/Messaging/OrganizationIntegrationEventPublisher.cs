using Amolenk.Admitto.Core.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.Messaging;

internal sealed class OrganizationIntegrationEventPublisher(
    [FromKeyedServices(OrganizationModuleKey.Value)] IOutbox outbox)
    : IDomainEventHandler<TicketedEventCreationRequestedDomainEvent>
{
    public ValueTask HandleAsync(
        TicketedEventCreationRequestedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        outbox.Enqueue(new TicketedEventCreationRequestedIntegrationEvent(
            domainEvent.CreationRequestId.Value,
            domainEvent.TeamId.Value,
            domainEvent.Name.Value,
            domainEvent.WebsiteUrl.Value.ToString(),
            domainEvent.BaseUrl.Value.ToString(),
            domainEvent.StartsAt,
            domainEvent.EndsAt,
            domainEvent.TimeZone.Value));

        return ValueTask.CompletedTask;
    }
}
