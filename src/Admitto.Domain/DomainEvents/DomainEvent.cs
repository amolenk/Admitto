namespace Amolenk.Admitto.Domain.DomainEvents;

public record DomainEvent
{
    public Guid DomainEventId { get; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}
