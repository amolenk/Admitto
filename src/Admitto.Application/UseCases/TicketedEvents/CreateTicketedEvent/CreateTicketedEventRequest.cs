using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

public record CreateTicketedEventRequest(
    string Name,
    Guid TeamId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    DateTimeOffset RegistrationStartTime,
    DateTimeOffset RegistrationEndTime,
    IEnumerable<TicketTypeDto> TicketTypes)
{
    public TicketedEvent ToTicketedEvent()
    {
        var ticketedEvent = TicketedEvent.Create(TeamId, Name, StartTime, EndTime, RegistrationStartTime,
            RegistrationEndTime);

        foreach (var ticketType in TicketTypes)
        {
            ticketedEvent.AddTicketType(ticketType.Name, ticketType.SlotName, ticketType.MaxCapacity);
        }
        
        return ticketedEvent;
    }
}

public record TicketTypeDto(string Name, string SlotName, int MaxCapacity);
