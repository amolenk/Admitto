using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Core.Registrations.Application.Messaging.EventHandlers;

internal sealed class AttendeeRegisteredDomainEventHandler(
    [FromKeyedServices(RegistrationsModule.Key)] IOutbox outbox)
    : IDomainEventHandler<AttendeeRegisteredDomainEvent>
{
    public ValueTask HandleAsync(AttendeeRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new AttendeeRegisteredIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.RegistrationId.Value,
            domainEvent.RecipientEmail.Value,
            domainEvent.FirstName.Value,
            domainEvent.LastName.Value,
            domainEvent.Tickets.Select(t => new TicketTypeItem(t.Slug, t.Name)).ToList()));

        return ValueTask.CompletedTask;
    }
}
