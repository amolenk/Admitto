namespace Amolenk.Admitto.Domain.DomainEvents;

public record RegistrationFailedDomainEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid AttendeeId) : DomainEvent;