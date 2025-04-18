using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

public record GetTicketedEventQuery(Guid TeamId, Guid TicketedEventId);

public record TicketedEventDto(
    Guid Id,
    string Name,
    DateOnly StartDay,
    DateOnly EndDay,
    DateTime SalesStartDateTime,
    DateTime SalesEndDateTime,
    IEnumerable<TicketTypeDto> TicketTypes)
{
    public static TicketedEventDto FromTicketedEvent(TicketedEvent ticketedEvent)
    {
        return new TicketedEventDto(
            ticketedEvent.Id,
            ticketedEvent.Name,
            ticketedEvent.StartDay,
            ticketedEvent.EndDay,
            ticketedEvent.SalesStartDateTime,
            ticketedEvent.SalesEndDateTime,
            ticketedEvent.TicketTypes.Select(TicketTypeDto.FromTicketType));
    }
}

public record TicketTypeDto(string Name, DateTime StartDateTime, DateTime EndDateTime, int MaxCapacity,
    int RemainingCapacity)
{
    public static TicketTypeDto FromTicketType(TicketType ticketType)
    {
        return new TicketTypeDto(ticketType.Name, ticketType.StartDateTime, ticketType.EndDateTime, 
            ticketType.MaxCapacity, ticketType.RemainingCapacity);
    }
}