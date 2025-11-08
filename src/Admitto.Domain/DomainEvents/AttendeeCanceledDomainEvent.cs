using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record AttendeeCanceledDomainEvent(
    Guid TicketedEventId,
    Guid ParticipantId,
    Guid AttendeeId,
    string Email,
    IList<TicketSelection> Tickets,
    CancellationReason? Reason = CancellationReason.Unknown)
    : DomainEvent;