namespace Amolenk.Admitto.Domain.DomainEvents;

public record AttendeeCheckedInDomainEvent(Guid TeamId, Guid TicketedEventId, Guid AttendeeId, uint AttendeeVersion)
    : DomainEvent;