namespace Amolenk.Admitto.Domain.DomainEvents;

public record TeamCreatedDomainEvent(Guid TeamId, string TeamSlug) : DomainEvent;