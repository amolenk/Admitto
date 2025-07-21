using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record AttendeeInvitedDomainEvent(Guid TicketedEventId, Guid AttendeeId, List<TicketSelection> Tickets)
    : DomainEvent;
