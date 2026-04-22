namespace Amolenk.Admitto.Module.Shared.Application.Messaging;

/// <summary>
/// Enqueues an <see cref="IIntegrationEvent"/> into the module's outbox, typically from
/// application-layer code paths that don't have a domain aggregate to hang a domain event
/// on (e.g. integration-event handlers that reject an incoming message).
/// </summary>
/// <remarks>
/// The message is persisted when the module's unit of work commits. The implementation uses
/// the same conventions as <c>OutboxWriter</c> for deriving the message type from the
/// integration event's CLR namespace.
/// </remarks>
public interface IIntegrationEventOutbox
{
    void Enqueue(IIntegrationEvent integrationEvent);
}
