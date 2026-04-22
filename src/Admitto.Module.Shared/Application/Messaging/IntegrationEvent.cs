namespace Amolenk.Admitto.Module.Shared.Application.Messaging;

public interface IIntegrationEvent
{
    Guid IntegrationEventId { get; }
}

public abstract record IntegrationEvent : IIntegrationEvent
{
    // Properties must be init-settable for deserialization.
    
    public Guid IntegrationEventId { get; init;  } = Guid.NewGuid();

    public DateTimeOffset OccurredOn { get; init;  } = DateTimeOffset.UtcNow;
}