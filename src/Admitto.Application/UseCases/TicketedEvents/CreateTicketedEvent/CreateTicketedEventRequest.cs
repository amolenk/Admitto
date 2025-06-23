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
        return TicketedEvent.Create(TeamId, Name, StartTime, EndTime, RegistrationStartTime, RegistrationEndTime);
    }
}

public record TicketTypeDto(string Name, string SlotName, int MaxCapacity)
{
    public TicketType ToTicketType()
    {
        return TicketType.Create(Name, SlotName, MaxCapacity);
    }
}