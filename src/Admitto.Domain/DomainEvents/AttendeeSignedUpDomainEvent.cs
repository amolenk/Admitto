namespace Amolenk.Admitto.Domain.DomainEvents;

public record AttendeeSignedUpDomainEvent(Guid TeamId, Guid TicketedEventId, Guid AttendeeId)
    : DomainEvent;
