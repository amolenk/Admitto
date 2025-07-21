namespace Amolenk.Admitto.Domain.DomainEvents;

public record TicketsReservedDomainEvent(Guid AttendeeId) : DomainEvent;