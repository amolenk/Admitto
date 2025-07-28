namespace Amolenk.Admitto.Application.Common.Core;

/// <summary>
/// Represents an event in the application layer.
/// </summary>
public abstract record ApplicationEvent
{
    public Guid DomainEventId { get; } = Guid.NewGuid();

    public DateTime OccurredOn { get; } = DateTime.Now;
}
