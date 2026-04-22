using Azure.Messaging;
using Azure.Storage.Queues;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Messaging;

/// <summary>
/// Polls an Azure Storage Queue for CloudEvents, invokes a handler per message,
/// and deletes the message after successful processing. Uses an exponentially
/// growing back-off (capped at <paramref name="maxPollDelay"/>) when the queue
/// is empty so the consumer doesn't hammer the Storage account.
/// </summary>
internal sealed partial class AzureStorageQueueProcessor(
    QueueClient queueClient,
    TimeSpan maxPollDelay,
    ILogger logger)
{
    public async ValueTask ProcessMessagesAsync(
        Func<CloudEvent, CancellationToken, ValueTask> messageHandler,
        CancellationToken cancellationToken)
    {
        var emptyPollCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var processed = await TryProcessNextMessageAsync(messageHandler, cancellationToken);
                if (processed)
                {
                    emptyPollCount = 0;
                    continue;
                }

                emptyPollCount++;
                var delayMs = Math.Min((int)maxPollDelay.TotalMilliseconds, emptyPollCount * 500);

                LogQueueEmpty(logger, queueClient.Name, delayMs);

                await Task.Delay(delayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                LogProcessorCancelled(logger, queueClient.Name);
                return;
            }
            catch (Exception ex)
            {
                LogProcessingError(logger, ex, queueClient.Name);
                // Brief pause to avoid hot-looping on a persistent failure;
                // the message stays on the queue (visibility timeout will return it).
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    private async ValueTask<bool> TryProcessNextMessageAsync(
        Func<CloudEvent, CancellationToken, ValueTask> messageHandler,
        CancellationToken cancellationToken)
    {
        var response = await queueClient.ReceiveMessageAsync(cancellationToken: cancellationToken);
        if (!response.HasValue || response.Value is null) return false;

        var message = response.Value;
        var cloudEvent = CloudEvent.Parse(message.Body)
                         ?? throw new InvalidOperationException(
                             $"Unable to parse CloudEvent from message {message.MessageId}.");

        await messageHandler(cloudEvent, cancellationToken);

        await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);

        return true;
    }

    [LoggerMessage(LogLevel.Debug, "Queue '{QueueName}' is empty. Waiting {DelayMs}ms before retrying...")]
    static partial void LogQueueEmpty(ILogger logger, string queueName, int delayMs);

    [LoggerMessage(LogLevel.Debug, "Processor for queue '{QueueName}' is canceled.")]
    static partial void LogProcessorCancelled(ILogger logger, string queueName);

    [LoggerMessage(LogLevel.Error, "Failed to process message from queue '{QueueName}'.")]
    static partial void LogProcessingError(ILogger logger, Exception exception, string queueName);
}
