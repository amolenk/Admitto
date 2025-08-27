namespace Amolenk.Admitto.Domain.DomainEvents;

public record SpeakerEngagementAddedDomainEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid EngagementId,
    uint EngagementVersion,
    string Email)
    : DomainEvent;
