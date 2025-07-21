namespace Amolenk.Admitto.Domain.DomainEvents;

public record RegistrationCompletedDomainEvent(Guid TeamId, Guid TicketedEventId, Guid AttendeeId) : DomainEvent;