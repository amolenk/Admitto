using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;

public interface IDomainEvent
{
    DomainEventId EventId { get; }

    DateTimeOffset OccurredOn { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    // Properties must be init-settable for deserialization.
    
    public DomainEventId EventId { get; init;  } = DomainEventId.New();

    public DateTimeOffset OccurredOn { get; init;  } = DateTimeOffset.UtcNow;
}
