namespace Amolenk.Admitto.Application.Common.Messaging;

/// <summary>
/// Marker interface for application event handlers.
/// </summary>
public interface IApplicationEventHandler;

/// <summary>
/// Represents an event handler for application events.
/// </summary>
public interface IApplicationEventHandler<in TApplicationEvent> : IApplicationEventHandler
    where TApplicationEvent : ApplicationEvent
{
    ValueTask HandleAsync(TApplicationEvent applicationEvent, CancellationToken cancellationToken);
}

