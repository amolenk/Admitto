namespace Amolenk.Admitto.Domain.DomainEvents;

public record AttendeeCanceledDomainEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid AttendeeId,
    bool LateCancellation,
    uint AttendeeVersion)
    : DomainEvent;