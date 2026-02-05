namespace Amolenk.Admitto.Registrations.Domain.ValueObjects;

public sealed record TicketTypeSnapshot(TicketTypeId Id, TimeSlot[] TimeSlots)
{
}