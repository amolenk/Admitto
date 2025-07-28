namespace Amolenk.Admitto.Domain.DomainEvents;

public record TicketsUnavailableDomainEvent(Guid AttendeeId) : DomainEvent;