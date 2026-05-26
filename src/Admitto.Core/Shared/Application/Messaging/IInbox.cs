namespace Amolenk.Admitto.Core.Shared.Application.Messaging;

/// <summary>
/// Marks a message as processed in the inbox. This ensures exactly-once processing.
public interface IInbox
{
    void MarkAsProcessed<THandler>(IIntegrationEvent integrationEvent, THandler handler);
}
