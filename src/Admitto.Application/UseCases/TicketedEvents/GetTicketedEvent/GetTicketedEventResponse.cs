using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

public record GetTicketedEventResponse(
    Guid Id,
    string Name,
    DateTimeOffset StartDateTime,
    DateTimeOffset EndDateTime,
    DateTimeOffset RegistrationStartDateTime,
    DateTimeOffset RegistrationEndDateTime,
    IEnumerable<TicketTypeDto> TicketTypes)
{
    public static GetTicketedEventResponse FromTicketedEvent(TicketedEvent ticketedEvent)
    {
        return new GetTicketedEventResponse(
            ticketedEvent.Id,
            ticketedEvent.Name,
            ticketedEvent.StartTime,
            ticketedEvent.EndTime,
            ticketedEvent.RegistrationStartTime,
            ticketedEvent.RegistrationEndTime,
            ticketedEvent.TicketTypes.Select(TicketTypeDto.FromTicketType));
    }
}

public record TicketTypeDto(string Name, string SlotName, int MaxCapacity, int RemainingCapacity)
{
    public static TicketTypeDto FromTicketType(TicketType ticketType)
    {
        return new TicketTypeDto(ticketType.Name, ticketType.SlotName, ticketType.MaxCapacity, 
            ticketType.RemainingCapacity);
    }
}