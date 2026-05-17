using Azure.Messaging;
using Azure.Messaging.ServiceBus;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;

/// <summary>
/// Thin wrapper around <see cref="ServiceBusProcessor"/> that registers message and error handlers.
/// On successful dispatch the message is explicitly completed; on exception the processor
/// auto-abandons so the message is retried up to its max delivery count, then dead-lettered.
/// </summary>
internal sealed partial class AzureServiceBusQueueProcessor(
    ServiceBusProcessor processor,
    Func<CloudEvent, CancellationToken, ValueTask> messageHandler,
    ILogger logger)
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        processor.ProcessMessageAsync += HandleMessageAsync;
        processor.ProcessErrorAsync += HandleErrorAsync;
        await processor.StartProcessingAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await processor.StopProcessingAsync(cancellationToken);
        processor.ProcessMessageAsync -= HandleMessageAsync;
        processor.ProcessErrorAsync -= HandleErrorAsync;
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var cloudEvent = CloudEvent.Parse(args.Message.Body)
                         ?? throw new InvalidOperationException(
                             $"Unable to parse CloudEvent from message {args.Message.MessageId}.");

        await messageHandler(cloudEvent, args.CancellationToken);

        await args.CompleteMessageAsync(args.Message, args.CancellationToken);
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        LogProcessingError(logger, args.Exception, processor.EntityPath);
        return Task.CompletedTask;
    }

    [LoggerMessage(LogLevel.Error, "Failed to process message from queue '{QueueName}'.")]
    static partial void LogProcessingError(ILogger logger, Exception exception, string queueName);
}
