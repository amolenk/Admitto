namespace Amolenk.Admitto.Domain.DomainEvents;

/// <summary>
/// Represents a domain event that is triggered when the registration is completed for an attendee.
/// </summary>
public record RegistrationCompletedDomainEvent(Guid TeamId, Guid TicketedEventId, Guid AttendeeId) : DomainEvent;