using Azure.Messaging;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Infrastructure.Messaging;

public class AzureStorageQueueProcessor(QueueClient queueClient, TimeSpan maxPollDelay, ILogger logger)
{
    public async ValueTask ProcessMessagesAsync(Func<CloudEvent, CancellationToken, ValueTask> messageHandler,
        CancellationToken cancellationToken)
    {
        var emptyPollCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var processedMessaged = await TryProcessNextMessageAsync(messageHandler, queueClient, cancellationToken);
                if (processedMessaged)
                {
                    // Great, we processed a message. Reset the empty poll count and continue with the next.
                    emptyPollCount = 0;
                    continue;
                }
                
                // No messages on the queue. Wait a bit before retrying based on the poll count.
                emptyPollCount++;
                var delayMs = Math.Min((int)maxPollDelay.TotalMilliseconds, emptyPollCount * 500);
                
                logger.LogDebug("Queue '{QueueName}' is empty. Waiting {Delay}ms before retrying...", queueClient.Name,
                    delayMs);
                
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug("Processor for queue '{QueueName}' is canceled.", queueClient.Name);
            }
        }
    }

    private static async ValueTask<bool> TryProcessNextMessageAsync(
        Func<CloudEvent, CancellationToken, ValueTask> messageHandler, QueueClient queueClient,
        CancellationToken cancellationToken)
    {
        var response = await queueClient.ReceiveMessageAsync(cancellationToken: cancellationToken);
        if (!response.HasValue || response.Value is null) return false;

        var message = response.Value;
        var cloudEvent = CloudEvent.Parse(message.Body)!;
        
        await messageHandler(cloudEvent, cancellationToken);

        await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);

        return true;
    }
}