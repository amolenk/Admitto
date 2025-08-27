using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.DomainEvents;

public record AttendeeCanceledDomainEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid RegistrationId,
    uint RegistrationVersion,
    string Email,
    IList<TicketSelection> Tickets)
    : DomainEvent;