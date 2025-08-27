using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record AttendeeCanceledLateDomainEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid RegistrationId,
    uint RegistrationVersion,
    string Email,
    IList<TicketSelection> Tickets)
    : DomainEvent;