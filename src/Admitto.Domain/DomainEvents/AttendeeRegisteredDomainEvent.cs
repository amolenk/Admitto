namespace Amolenk.Admitto.Domain.DomainEvents;

/// <summary>
/// Represents a domain event that is triggered when a person registers for a ticketed event.
/// </summary>
public record AttendeeRegisteredDomainEvent(
    Guid TicketedEventId,
    Guid ParticipantId,
    Guid AttendeeId,
    string Email,
    string FirstName,
    string LastName)
    : DomainEvent;