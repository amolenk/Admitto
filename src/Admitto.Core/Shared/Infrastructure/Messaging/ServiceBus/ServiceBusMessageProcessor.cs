using Azure.Messaging;
using Azure.Messaging.ServiceBus;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Messaging.ServiceBus;

/// <summary>
/// Hosted service that consumes the Service Bus queue and routes each received CloudEvent through
/// the <see cref="QueueMessageDispatcher"/>. Registered only by hosts that opt in (the Worker today).
/// <para>
/// Delivery is push-based: the broker pushes messages over a long-lived AMQP link that the SDK's
/// <see cref="ServiceBusProcessor"/> owns, keeps alive, and re-establishes after a fault. It also
/// renews the message lock while a handler runs, so a slow handler no longer risks losing its lock
/// and having the message redelivered.
/// </para>
/// <para>
/// Messages are settled explicitly (<see cref="ServiceBusProcessorOptions.AutoCompleteMessages"/> is
/// off): completed once the dispatcher succeeds, abandoned for redelivery when it fails. A message
/// that keeps failing is dead-lettered by the broker once it exceeds the queue's max delivery count.
/// </para>
/// </summary>
internal sealed partial class ServiceBusMessageProcessor : IHostedService, IAsyncDisposable
{
    private const string QueueName = "queue";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ServiceBusMessageProcessor> _logger;
    private readonly ServiceBusProcessor _processor;

    public ServiceBusMessageProcessor(
        ServiceBusClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<ServiceBusMessageProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _processor = client.CreateProcessor(QueueName, new ServiceBusProcessorOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            AutoCompleteMessages = false,
            // Dispatch stays strictly sequential; handlers have never had to tolerate concurrent
            // delivery on a single Worker replica.
            MaxConcurrentCalls = 1
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogStarting(_logger, QueueName);

        await _processor.StartProcessingAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_processor.IsProcessing) return;

        await _processor.StopProcessingAsync(cancellationToken);
    }

    public ValueTask DisposeAsync() => _processor.DisposeAsync();

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message;

        try
        {
            var cloudEvent = CloudEvent.Parse(message.Body)
                ?? throw new InvalidOperationException(
                    $"Unable to parse CloudEvent from message {message.MessageId}.");

            using var scope = _scopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<QueueMessageDispatcher>();
            await dispatcher.DispatchAsync(cloudEvent, args.CancellationToken);

            await args.CompleteMessageAsync(message, args.CancellationToken);
        }
        catch (OperationCanceledException) when (args.CancellationToken.IsCancellationRequested)
        {
            // Worker shutting down — let the message lock expire so it is redelivered.
        }
        catch (Exception ex)
        {
            LogProcessingFailed(_logger, QueueName, ex);

            await AbandonAsync(args, message);
        }
    }

    private async Task AbandonAsync(ProcessMessageEventArgs args, ServiceBusReceivedMessage message)
    {
        try
        {
            await args.AbandonMessageAsync(message, cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            LogAbandonFailed(_logger, message.MessageId, ex);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        if (args.CancellationToken.IsCancellationRequested) return Task.CompletedTask;

        // The processor reconnects on its own after link and connection faults, so a single
        // occurrence is not actionable; a genuine outage shows up as the warning repeating.
        LogProcessorError(_logger, args.ErrorSource, QueueName, args.Exception);

        return Task.CompletedTask;
    }

    [LoggerMessage(
        LogLevel.Information,
        "Starting message queue processor for queue '{QueueName}'.")]
    static partial void LogStarting(ILogger<ServiceBusMessageProcessor> logger, string queueName);

    [LoggerMessage(
        LogLevel.Error,
        "Failed to process message from queue '{QueueName}'.")]
    static partial void LogProcessingFailed(
        ILogger<ServiceBusMessageProcessor> logger,
        string queueName,
        Exception exception);

    [LoggerMessage(
        LogLevel.Warning,
        "Failed to abandon message {MessageId}.")]
    static partial void LogAbandonFailed(
        ILogger<ServiceBusMessageProcessor> logger,
        string messageId,
        Exception exception);

    [LoggerMessage(
        LogLevel.Warning,
        "Service Bus processor error from {ErrorSource} on queue '{QueueName}'; the processor will retry.")]
    static partial void LogProcessorError(
        ILogger<ServiceBusMessageProcessor> logger,
        ServiceBusErrorSource errorSource,
        string queueName,
        Exception exception);
}
