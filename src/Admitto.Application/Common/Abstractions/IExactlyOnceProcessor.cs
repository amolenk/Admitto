namespace Amolenk.Admitto.Application.Common.Abstractions;

/// <summary>
/// Service for managing exactly-once message processing.
/// </summary>
public interface IExactlyOnceProcessor
{
    /// <summary>
    /// Attempts to mark a message as processed. Returns true if the message was successfully marked
    /// (meaning it hadn't been processed before), or false if it was already processed.
    /// </summary>
    /// <param name="messageId">The unique identifier of the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the message is new and was marked as processed, false if already processed</returns>
    ValueTask<bool> TryMarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
}