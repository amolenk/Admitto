namespace Amolenk.Admitto.Module.Shared.Application.Messaging;

public interface IModuleEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOn { get; }
}

public abstract record ModuleEvent : IModuleEvent
{
    // Properties must be init-settable for deserialization.
    
    public Guid EventId { get; init;  } = Guid.NewGuid();

    public DateTimeOffset OccurredOn { get; init;  } = DateTimeOffset.UtcNow;
}