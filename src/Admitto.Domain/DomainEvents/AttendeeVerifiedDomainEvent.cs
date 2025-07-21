using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record AttendeeVerifiedDomainEvent(Guid TicketedEventId, Guid AttendeeId, List<TicketSelection> Tickets)
    : DomainEvent;