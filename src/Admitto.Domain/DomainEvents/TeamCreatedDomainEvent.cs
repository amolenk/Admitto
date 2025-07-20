namespace Amolenk.Admitto.Domain.DomainEvents;

public record TeamCreatedDomainEvent(Guid TeamId, string TeamSlug) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}