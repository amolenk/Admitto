namespace Amolenk.Admitto.Domain.DomainEvents;

public record AttendeeNoShowDomainEvent(Guid TeamId, Guid TicketedEventId, Guid AttendeeId, uint AttendeeVersion)
    : DomainEvent;
