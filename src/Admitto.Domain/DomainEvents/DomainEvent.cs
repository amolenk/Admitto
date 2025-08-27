namespace Amolenk.Admitto.Domain.DomainEvents;

public record DomainEvent
{
    // Properties must be init-settable for deserialization.
    
    public Guid DomainEventId { get; init; } = Guid.NewGuid();

    public DateTimeOffset OccurredOn { get; init;  } = DateTimeOffset.UtcNow;
}
