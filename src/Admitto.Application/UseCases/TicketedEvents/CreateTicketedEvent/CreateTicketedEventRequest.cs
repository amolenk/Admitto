using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

public record CreateTicketedEventRequest(
    string Name,
    DateTimeOffset StartDateTime,
    DateTimeOffset EndDateTime,
    DateTimeOffset RegistrationStartDateTime,
    DateTimeOffset RegistrationEndDateTime,
    IEnumerable<TicketTypeDto> TicketTypes)
{
    public TicketedEvent ToTicketedEvent()
    {
        return TicketedEvent.Create(Name, StartDateTime, EndDateTime, RegistrationStartDateTime,
            RegistrationEndDateTime, TicketTypes.Select(tt => tt.ToTicketType()));
    }
}

public record TicketTypeDto(string Name, string SlotName, int MaxCapacity)
{
    public TicketType ToTicketType()
    {
        return TicketType.Create(Name, SlotName, MaxCapacity);
    }
}