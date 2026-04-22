using Azure.Storage.Queues;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Messaging;

/// <summary>
/// Hosted service that drains the outbox queue and dispatches each CloudEvent
/// through the <see cref="QueueMessageDispatcher"/>. Registered only by hosts that
/// opt in (the Worker today).
/// </summary>
internal sealed class MessageQueueProcessor(
    QueueClient queueClient,
    IServiceScopeFactory scopeFactory,
    ILogger<MessageQueueProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan MaxPollDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting message queue processor for queue '{QueueName}'.", queueClient.Name);

        var processor = new AzureStorageQueueProcessor(queueClient, MaxPollDelay, logger);

        await processor.ProcessMessagesAsync(
            async (cloudEvent, ct) =>
            {
                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<QueueMessageDispatcher>();
                await dispatcher.DispatchAsync(cloudEvent, ct);
            },
            stoppingToken);
    }
}
