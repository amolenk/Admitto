namespace Amolenk.Admitto.Application.Common.Messaging;

/// <summary>
/// Represents an event in the application layer.
/// </summary>
public abstract record ApplicationEvent
{
    // Properties must be init-settable for deserialization.

    public Guid ApplicationEventId { get; init;  } = Guid.NewGuid();

    public DateTimeOffset OccurredOn { get; init;  } = DateTimeOffset.UtcNow;
}
