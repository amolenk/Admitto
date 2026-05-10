using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Core.Registrations.Application.Messaging.EventHandlers;

internal sealed class TicketedEventTimeZoneChangedDomainEventHandler(
    [FromKeyedServices(RegistrationsModule.Key)] IOutbox outbox)
    : IDomainEventHandler<TicketedEventTimeZoneChangedDomainEvent>
{
    public ValueTask HandleAsync(
        TicketedEventTimeZoneChangedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        outbox.Enqueue(new TicketedEventTimeZoneChangedIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.TimeZone.Value));

        return ValueTask.CompletedTask;
    }
}
