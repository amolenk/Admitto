using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetActiveTicketedEvents;

public record GetActiveTicketedEventsResponse(TicketedEventDto[] TicketedEvents)
{
    public static GetActiveTicketedEventsResponse FromTicketedEvents(IEnumerable<TicketedEvent> ticketedEvents)
    {
        return new GetActiveTicketedEventsResponse(ticketedEvents.Select(TicketedEventDto.FromTicketedEvent).ToArray());
    }
}

public record TicketedEventDto(Guid Id, string Name)
{
    public static TicketedEventDto FromTicketedEvent(TicketedEvent ticketedEvent)
    {
        return new TicketedEventDto(ticketedEvent.Id, ticketedEvent.Name);
    }
}