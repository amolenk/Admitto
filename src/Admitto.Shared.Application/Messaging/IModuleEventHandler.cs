namespace Amolenk.Admitto.Shared.Application.Messaging;

/// <summary>
/// Represents a module event handler that runs after events has been published through the outbox.
/// </summary>
public interface IModuleEventHandler<in TModuleEvent>
    where TModuleEvent : IModuleEvent
{
    ValueTask HandleAsync(TModuleEvent moduleEvent, CancellationToken cancellationToken);
}
