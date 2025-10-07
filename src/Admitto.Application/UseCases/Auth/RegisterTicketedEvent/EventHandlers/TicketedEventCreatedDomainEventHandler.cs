using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.UseCases.Auth.RegisterTicketedEvent.EventHandlers;

public class TicketedEventCreatedDomainEventHandler(RegisterTicketedEventHandler registerTicketedEventHandler)
    : IEventualDomainEventHandler<TicketedEventCreatedDomainEvent>
{
    public ValueTask HandleAsync(TicketedEventCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new RegisterTicketedEventCommand(domainEvent.TeamId, domainEvent.TicketedEventId);

        return registerTicketedEventHandler.HandleAsync(command, cancellationToken);
    }
}
