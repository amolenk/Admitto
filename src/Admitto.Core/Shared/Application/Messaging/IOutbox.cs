namespace Amolenk.Admitto.Core.Shared.Application.Messaging;

/// <summary>
/// Enqueues a <see cref="ICommand"/> or <see cref="IIntegrationEvent"/> into the module's
/// outbox for deferred/cross-module delivery.
/// </summary>
/// <remarks>
/// The message is persisted when the module's unit of work commits.
/// Inject the keyed service using <c>[FromKeyedServices("&lt;moduleKey&gt;")]</c>.
/// </remarks>
public interface IOutbox
{
    void Enqueue(ICommand command);
    void Enqueue(IIntegrationEvent integrationEvent);
}
