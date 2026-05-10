using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Amolenk.Admitto.Core.Registrations.Application.Messaging.EventHandlers;

internal sealed class TicketedEventStatusChangedDomainEventHandler(
    [FromKeyedServices(RegistrationsModule.Key)] IOutbox outbox)
    : IDomainEventHandler<TicketedEventStatusChangedDomainEvent>
{
    public ValueTask HandleAsync(TicketedEventStatusChangedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        IIntegrationEvent integrationEvent = domainEvent.NewStatus switch
        {
            EventLifecycleStatus.Cancelled => new TicketedEventCancelledIntegrationEvent(
                domainEvent.TeamId.Value,
                domainEvent.TicketedEventId.Value),
            EventLifecycleStatus.Archived => new TicketedEventArchivedIntegrationEvent(
                domainEvent.TeamId.Value,
                domainEvent.TicketedEventId.Value),
            _ => throw new InvalidOperationException(
                $"Unexpected {nameof(EventLifecycleStatus)} '{domainEvent.NewStatus}' for " +
                $"{nameof(TicketedEventStatusChangedDomainEvent)}.")
        };

        outbox.Enqueue(integrationEvent);

        return ValueTask.CompletedTask;
    }
}
