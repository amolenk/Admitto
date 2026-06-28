using Amolenk.Admitto.Core.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.Messaging;

internal sealed class OrganizationIntegrationEventPublisher(
    [FromKeyedServices(OrganizationModule.Key)] IOutbox outbox)
    : IDomainEventHandler<TicketedEventCreationRequestedDomainEvent>,
      IDomainEventHandler<TeamCreatedDomainEvent>,
      IDomainEventHandler<TeamDetailsUpdatedDomainEvent>
{
    public ValueTask HandleAsync(
        TicketedEventCreationRequestedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        outbox.Enqueue(new TicketedEventCreationRequestedIntegrationEvent(
            domainEvent.CreationRequestId.Value,
            domainEvent.TeamId.Value,
            domainEvent.Name.Value,
            domainEvent.WebsiteUrl.Value,
            domainEvent.BaseUrl.Value,
            domainEvent.StartsAt,
            domainEvent.EndsAt,
            domainEvent.TimeZone.Value,
            domainEvent.PublicSlug.Value));

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(TeamCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new TeamCreatedIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.Name.Value,
            domainEvent.AccentColor.Value,
            domainEvent.ReplyToEmailAddress?.Value,
            domainEvent.TeamVersion));

        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(TeamDetailsUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new TeamDetailsUpdatedIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.Name.Value,
            domainEvent.AccentColor.Value,
            domainEvent.ReplyToEmailAddress?.Value,
            domainEvent.TeamVersion));

        return ValueTask.CompletedTask;
    }
}
