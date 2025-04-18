using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetActiveEvents;

public record GetActiveEventsQuery(Guid TeamId);

public record TicketedEventDto(Guid Id, string Name)
{
    public static TicketedEventDto FromTicketedEvent(TicketedEvent ticketedEvent)
    {
        return new TicketedEventDto(ticketedEvent.Id, ticketedEvent.Name);
    }
}
