namespace Amolenk.Admitto.Application.MessageOutbox;

/// <summary>
/// Classes that implement this interface can provide outbox messages to be processed.
/// </summary>
public interface IOutboxMessageProvider
{
    Task ExecuteAsync(
        Func<OutboxMessage, CancellationToken, ValueTask> handleMessageAsync, 
        CancellationToken cancellationToken);
}