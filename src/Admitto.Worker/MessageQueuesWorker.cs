using System.Text.Json;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Infrastructure.Messaging;
using Azure.Messaging;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Telemetry;

namespace Amolenk.Admitto.Worker;

/// <summary>
/// Receives cloud events from the Azure Storage queues and dispatches them to the appropriate handlers.
/// </summary>
public class MessageQueuesWorker(
    [FromKeyedServices("queues")] QueueServiceClient queueServiceClient,
    IServiceProvider serviceProvider,
    IOptions<MessageQueuesWorkerOptions> options,
    ILoggerFactory loggerFactory,
    ILogger<MessageQueuesWorker> logger)
    : BackgroundService
{
    private readonly AzureStorageQueueProcessor _queueProcessor = new(
        queueServiceClient.GetQueueClient("queue"), options.Value.MaxPollDelay, logger);
    
    private readonly AzureStorageQueueProcessor _prioQueueProcessor = new(
        queueServiceClient.GetQueueClient("queue-prio"), options.Value.MaxPrioPollDelay, logger);
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueTask = CreateRetryStrategy("Queue").ExecuteAsync(
            ct => _queueProcessor.ProcessMessagesAsync(HandleMessageAsync, ct), stoppingToken).AsTask();

        var prioQueueTask = CreateRetryStrategy("PrioQueue").ExecuteAsync(
            ct => _prioQueueProcessor.ProcessMessagesAsync(HandleMessageAsync, ct), stoppingToken).AsTask();

        return Task.WhenAll(queueTask, prioQueueTask);
    }

    private async ValueTask HandleMessageAsync(CloudEvent cloudEvent, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize(cloudEvent.Data!.ToString(), GetType(cloudEvent.Type),
            JsonSerializerOptions.Web);

        using var scope = serviceProvider.CreateScope();
        
        // Parse the message ID from the CloudEvent
        var messageId = Guid.Parse(cloudEvent.Id);
        
        try
        {
            switch (message)
            {
                case ICommand command:
                    await HandleCommandAsync(command, messageId, scope.ServiceProvider, cancellationToken);
                    break;
                case IDomainEvent domainEvent:
                    await HandleDomainEventAsync(domainEvent, messageId, scope.ServiceProvider, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Cannot handle outbox message of type: {cloudEvent.Type}");
            }

            // Commit the unit of work for this message
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ProcessedMessageDuplicateException)
        {
            // Message was already processed by another instance, this is expected for exactly-once processing
            // Log at debug level and continue
            logger.LogDebug("Message {MessageId} was already processed, skipping", messageId);
        }
    }

    private static Type GetType(string typeName)
    {
        if (typeName.EndsWith("Command"))
        {
            return Type.GetType($"Amolenk.Admitto.Application.UseCases.{typeName}, Admitto.Application", 
                true)!;
        }
        
        if (typeName.EndsWith("DomainEvent"))
        {
            return Type.GetType($"Amolenk.Admitto.Domain.DomainEvents.{typeName}, Admitto.Domain", 
                true)!;
        }

        throw new ArgumentException($"Unknown message type '{typeName}'", nameof(typeName));
    }
    
    private static async ValueTask HandleCommandAsync(ICommand command, Guid messageId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        dynamic handler = serviceProvider.GetRequiredService(handlerType);

        // Check if the handler implements exactly-once processing
        if (handler is IProcessMessagesExactlyOnce)
        {
            var exactlyOnceProcessor = serviceProvider.GetRequiredService<IExactlyOnceProcessor>();
            var shouldProcess = await exactlyOnceProcessor.TryMarkAsProcessedAsync(messageId, cancellationToken);
            
            if (!shouldProcess)
            {
                // Message was already processed, skip execution
                return;
            }
        }

        await handler.HandleAsync((dynamic)command, cancellationToken);
    }
    
    private static async ValueTask HandleDomainEventAsync(IDomainEvent domainEvent, Guid messageId, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IEventualDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        dynamic handler = serviceProvider.GetRequiredService(handlerType);

        // Check if the handler implements exactly-once processing
        if (handler is IProcessMessagesExactlyOnce)
        {
            var exactlyOnceProcessor = serviceProvider.GetRequiredService<IExactlyOnceProcessor>();
            var shouldProcess = await exactlyOnceProcessor.TryMarkAsProcessedAsync(messageId, cancellationToken);
            
            if (!shouldProcess)
            {
                // Message was already processed, skip execution
                return;
            }
        }

        await handler.HandleAsync((dynamic)domainEvent, cancellationToken);
    }
    
    private ResiliencePipeline CreateRetryStrategy(string instanceName)
    {
        var telemetryOptions = new TelemetryOptions
        {
            LoggerFactory = loggerFactory
        };

        var workerOptions = options.Value;
        
        return new ResiliencePipelineBuilder { Name = nameof(MessageQueuesWorker), InstanceName = instanceName}
            .AddRetry(new RetryStrategyOptions
            {
                BackoffType = workerOptions.RetryBackoffType,
                MaxDelay = workerOptions.MaxRetryDelay,
                MaxRetryAttempts = workerOptions.MaxRetryAttempts
            })
            .ConfigureTelemetry(telemetryOptions)
            .Build();
    }
}
