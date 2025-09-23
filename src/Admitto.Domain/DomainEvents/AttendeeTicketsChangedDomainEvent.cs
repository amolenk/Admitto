namespace Amolenk.Admitto.Domain.DomainEvents;

/// <summary>
/// Represents a domain event that is triggered when the tickets for an attendee have changed.
/// </summary>
public record AttendeeTicketsChangedDomainEvent(
    Guid TicketedEventId,
    Guid ParticipantId,
    Guid AttendeeId)
    : DomainEvent;