namespace Amolenk.Admitto.Domain.DomainEvents;

public record TicketedEventCreatedDomainEvent(Guid TeamId, string EventSlug) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}