namespace Amolenk.Admitto.Domain.DomainEvents;

public record SpeakerAddedDomainEvent(Guid TicketedEventId, Guid SpeakerId) : DomainEvent;
