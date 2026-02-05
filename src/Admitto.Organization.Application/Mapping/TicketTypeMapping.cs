using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Organization.Contracts;

namespace Amolenk.Admitto.Registrations.Application.Mapping;

internal static class TicketTypeMapping
{
    public static TicketTypeDto ToDto(this TicketTypeRecord ticketTypeRecord) => new()
    {
        Id = ticketTypeRecord.Id,
        AdminLabel = ticketTypeRecord.AdminLabel,
        PublicTitle = ticketTypeRecord.PublicTitle, 
        TimeSlots = ticketTypeRecord.TimeSlots.ToArray()
    };
}