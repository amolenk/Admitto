using Azure.Messaging;
using Azure.Messaging.ServiceBus;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Messaging.ServiceBus;

/// <summary>
/// Hosted service that polls the Service Bus queue using an explicit receive loop and routes each
/// received CloudEvent through the <see cref="QueueMessageDispatcher"/>. Registered only by hosts
/// that opt in (the Worker today).
/// <para>
/// An explicit polling loop (rather than <see cref="ServiceBusProcessor"/>) is used so that each
/// call to <see cref="ServiceBusReceiver.ReceiveMessageAsync"/> issues a fresh AMQP credit to the
/// broker. The Azure SB emulator only checks its MSSQL backend for new messages on fresh credits,
/// so passive listeners can miss messages for up to the default TryTimeout (60 s). With a 5-second
/// poll interval this is bounded to 5 s in all environments including the emulator.
/// </para>
/// <para>
/// The Aspire-registered <see cref="ServiceBusClient"/> is used so local emulator connection
/// strings and published Azure managed identity endpoints are both handled by the same client
/// registration. After <see cref="MaxConsecutiveNullReceives"/> consecutive null results, the
/// receiver is recreated to refresh the AMQP link when the SB emulator silently stalls delivery.
/// </para>
/// </summary>
internal sealed class ServiceBusMessageProcessor(
    ServiceBusClient client,
    IServiceScopeFactory scopeFactory,
    ILogger<ServiceBusMessageProcessor> logger) : BackgroundService
{
    private const string QueueName = "queue";
    private static readonly TimeSpan ReceiveWaitTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Recreate the receiver after this many consecutive null receives (~30 s idle)
    /// to refresh the AMQP link when the SB emulator silently stalls delivery.
    /// </summary>
    private const int MaxConsecutiveNullReceives = 6;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting message queue processor for queue '{QueueName}'.", QueueName);

        var receiver = CreateReceiver();
        var consecutiveNulls = 0;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ServiceBusReceivedMessage? message;
                try
                {
                    message = await receiver.ReceiveMessageAsync(
                        maxWaitTime: ReceiveWaitTime,
                        cancellationToken: stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Transient error receiving from queue '{QueueName}'; recreating receiver.", QueueName);
                    await DisposeReceiverAsync(receiver);
                    receiver = CreateReceiver();
                    consecutiveNulls = 0;
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                if (message == null)
                {
                    consecutiveNulls++;
                    if (consecutiveNulls >= MaxConsecutiveNullReceives)
                    {
                        logger.LogDebug(
                            "Recreating receiver after {Count} consecutive empty polls to refresh the AMQP link.",
                            consecutiveNulls);
                        await DisposeReceiverAsync(receiver);
                        receiver = CreateReceiver();
                        consecutiveNulls = 0;
                    }
                    continue;
                }

                consecutiveNulls = 0;

                try
                {
                    var cloudEvent = CloudEvent.Parse(message.Body)
                        ?? throw new InvalidOperationException(
                            $"Unable to parse CloudEvent from message {message.MessageId}.");

                    using var scope = scopeFactory.CreateScope();
                    var dispatcher = scope.ServiceProvider.GetRequiredService<QueueMessageDispatcher>();
                    await dispatcher.DispatchAsync(cloudEvent, stoppingToken);

                    await receiver.CompleteMessageAsync(message, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Worker shutting down — let the message lock expire so it is re-queued.
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process message from queue '{QueueName}'.", QueueName);
                    try
                    {
                        await receiver.AbandonMessageAsync(message, cancellationToken: CancellationToken.None);
                    }
                    catch (Exception abandonEx)
                    {
                        logger.LogWarning(abandonEx, "Failed to abandon message {MessageId}.", message.MessageId);
                    }
                }
            }
        }
        finally
        {
            await DisposeReceiverAsync(receiver);
        }
    }

    private ServiceBusReceiver CreateReceiver()
    {
        return client.CreateReceiver(QueueName, new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        });
    }

    private async ValueTask DisposeReceiverAsync(ServiceBusReceiver receiver)
    {
        try { await receiver.DisposeAsync(); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to dispose receiver for queue '{QueueName}'.", QueueName); }
    }
}
