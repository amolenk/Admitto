using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Core.Registrations.Application.Messaging.EventHandlers;

internal sealed class RegistrationReconfirmedDomainEventHandler(
    [FromKeyedServices(RegistrationsModule.Key)] IOutbox outbox)
    : IDomainEventHandler<RegistrationReconfirmedDomainEvent>
{
    public ValueTask HandleAsync(RegistrationReconfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new RegistrationReconfirmedIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.RegistrationId.Value,
            domainEvent.Email.Value,
            domainEvent.ReconfirmedAt));

        return ValueTask.CompletedTask;
    }
}
