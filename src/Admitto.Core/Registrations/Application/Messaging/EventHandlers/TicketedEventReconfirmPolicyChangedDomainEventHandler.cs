using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Core.Registrations.Application.Messaging.EventHandlers;

internal sealed class TicketedEventReconfirmPolicyChangedDomainEventHandler(
    [FromKeyedServices(RegistrationsModule.Key)] IOutbox outbox)
    : IDomainEventHandler<TicketedEventReconfirmPolicyChangedDomainEvent>
{
    public ValueTask HandleAsync(
        TicketedEventReconfirmPolicyChangedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        outbox.Enqueue(new TicketedEventReconfirmPolicyChangedIntegrationEvent(
            domainEvent.TeamId.Value,
            domainEvent.TicketedEventId.Value,
            domainEvent.Policy is null
                ? null
                : new TicketedEventReconfirmPolicySnapshot(
                    domainEvent.Policy.OpensAt,
                    domainEvent.Policy.ClosesAt,
                    (int)domainEvent.Policy.Cadence.TotalDays)));

        return ValueTask.CompletedTask;
    }
}
