using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.DTOs;

public record TicketTypeDto(string Name, DateTime StartDateTime, DateTime EndDateTime, int MaxCapacity)
{
    public static TicketTypeDto FromTicketType(TicketType ticketType)
    {
        throw new NotImplementedException();
    }
}
