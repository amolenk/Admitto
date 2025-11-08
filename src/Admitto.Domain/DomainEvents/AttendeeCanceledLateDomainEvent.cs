using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record AttendeeCanceledLateDomainEvent(
    Guid TicketedEventId,
    Guid ParticipantId,
    Guid AttendeeId,
    string Email,
    IList<TicketSelection> Tickets,
    CancellationReason? Reason = CancellationReason.Unknown)
    : DomainEvent;