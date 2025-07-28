using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Infrastructure.Messaging;

public class AzureServiceBusQueueProcessor(ServiceBusClient serviceBusClient, string queueName, ILogger logger)
{
    private readonly ServiceBusProcessor _processor = serviceBusClient.CreateProcessor(
        queueName,
        new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false
        });

    public async ValueTask RunAsync(
        Func<CloudEvent, CancellationToken, ValueTask> messageHandler,
        CancellationToken cancellationToken)
    {
        _processor.ProcessMessageAsync += async args =>
        {
            try
            {
                var cloudEvent = CloudEvent.Parse(args.Message.Body)!;
                await messageHandler(cloudEvent, cancellationToken);
                await args.CompleteMessageAsync(args.Message, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Service Bus message");
                await args.AbandonMessageAsync(args.Message, cancellationToken: cancellationToken);
            }
        };

        _processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Service Bus processor error");
            return Task.CompletedTask;
        };

        await _processor.StartProcessingAsync(cancellationToken);
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellationToken is cancelled
        }
        finally
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }
    }
}