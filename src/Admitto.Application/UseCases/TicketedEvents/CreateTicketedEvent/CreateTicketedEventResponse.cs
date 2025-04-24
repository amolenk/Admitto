using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

public record CreateTicketedEventResponse(Guid Id)
{
    public static CreateTicketedEventResponse FromTicketedEvent(TicketedEvent ticketedEvent)
    {
        return new CreateTicketedEventResponse(ticketedEvent.Id);
    }
}
