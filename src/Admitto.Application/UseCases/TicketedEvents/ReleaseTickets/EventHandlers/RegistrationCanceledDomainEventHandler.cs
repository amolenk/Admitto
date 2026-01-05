using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.ReleaseTickets.EventHandlers;

/// <summary>
/// When a registration is canceled, the tickets that were reserved for that registration should be released back to
/// the pool of available tickets.
/// </summary>
public class RegistrationCanceledDomainEventHandler(ReleaseTicketsHandler releaseTicketsHandler)
    : IEventualDomainEventHandler<AttendeeCanceledDomainEvent>
{
    public async ValueTask HandleAsync(AttendeeCanceledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new ReleaseTicketsCommand(domainEvent.TicketedEventId, domainEvent.Tickets)
        {
            CommandId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(ReleaseTicketsCommand)}")
        };

        await releaseTicketsHandler.HandleAsync(command, cancellationToken);
    }
}