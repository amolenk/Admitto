using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Registrations.Application.Mapping;

internal static class TicketTypeMapping
{
    public static TicketTypeSnapshot ToDomain(this TicketTypeDto dto) => new(
        new TicketTypeId(dto.Id),
        dto.TimeSlots.Select(TimeSlot.From).ToArray());
}