using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.RemoveAttendee.EventHandlers;

/// <summary>
/// Represents an event handler that removes the attendee from an event after a registration is canceled.
/// </summary>
public class RegistrationCanceledDomainEventHandler(RemoveAttendeeHandler removeAttendeeHandler)
    : IEventualDomainEventHandler<AttendeeCanceledDomainEvent>
{
    public async ValueTask HandleAsync(AttendeeCanceledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new RemoveAttendeeCommand(domainEvent.TicketedEventId, domainEvent.Email, domainEvent.Tickets)
        {
            CommandId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(RemoveAttendeeCommand)}")
        };

        await removeAttendeeHandler.HandleAsync(command, cancellationToken);
    }
}