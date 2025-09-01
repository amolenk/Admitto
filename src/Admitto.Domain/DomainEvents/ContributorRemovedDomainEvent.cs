namespace Amolenk.Admitto.Domain.DomainEvents;

public record ContributorRemovedDomainEvent(
    Guid TicketedEventId,
    Guid ParticipantId,
    Guid ContributorId,
    string Email)
    : DomainEvent;
