using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Core.Registrations.Application.Messaging.EventHandlers;

internal sealed class RegistrationCancelledDomainEventHandler(
    [FromKeyedServices(RegistrationsModule.Key)] IOutbox outbox)
    : IDomainEventHandler<RegistrationCancelledDomainEvent>
{
    public ValueTask HandleAsync(RegistrationCancelledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new RegistrationCancelledIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.RegistrationId.Value,
            domainEvent.Email.Value,
            domainEvent.Reason.ToString()));

        return ValueTask.CompletedTask;
    }
}
