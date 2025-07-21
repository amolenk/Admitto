namespace Amolenk.Admitto.Domain.DomainEvents;

public record RegistrationRejectedDomainEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid AttendeeId) : DomainEvent;