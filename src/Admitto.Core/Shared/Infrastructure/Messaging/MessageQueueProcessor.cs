using Azure.Messaging.ServiceBus;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;

/// <summary>
/// Hosted service that starts the <see cref="ServiceBusProcessor"/> and routes each received
/// CloudEvent through the <see cref="QueueMessageDispatcher"/>. Registered only by hosts that
/// opt in (the Worker today).
/// </summary>
internal sealed class MessageQueueProcessor(
    ServiceBusProcessor serviceBusProcessor,
    IServiceScopeFactory scopeFactory,
    ILogger<MessageQueueProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting message queue processor for queue '{QueueName}'.", serviceBusProcessor.EntityPath);

        var processor = new AzureServiceBusQueueProcessor(
            serviceBusProcessor,
            async (cloudEvent, ct) =>
            {
                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<QueueMessageDispatcher>();
                await dispatcher.DispatchAsync(cloudEvent, ct);
            },
            logger);

        await processor.StartAsync(stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        await processor.StopAsync(CancellationToken.None);
    }
}
