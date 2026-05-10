using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Core.Registrations.Application.Messaging.EventHandlers;

internal sealed class TicketsChangedDomainEventHandler(
    [FromKeyedServices(RegistrationsModule.Key)] IOutbox outbox)
    : IDomainEventHandler<TicketsChangedDomainEvent>
{
    public ValueTask HandleAsync(TicketsChangedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new AttendeeTicketsChangedIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.RegistrationId.Value,
            domainEvent.RecipientEmail.Value,
            domainEvent.FirstName.Value,
            domainEvent.LastName.Value,
            domainEvent.NewTickets.Select(t => new TicketTypeItem(t.Slug, t.Name)).ToList(),
            domainEvent.ChangedAt));

        return ValueTask.CompletedTask;
    }
}
