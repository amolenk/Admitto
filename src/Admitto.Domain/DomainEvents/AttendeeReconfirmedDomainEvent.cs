namespace Amolenk.Admitto.Domain.DomainEvents;

public record AttendeeReconfirmedDomainEvent(
    Guid TicketedEventId,
    Guid ParticipantId,
    Guid AttendeeId)
    : DomainEvent;