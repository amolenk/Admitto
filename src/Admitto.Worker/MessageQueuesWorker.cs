using System.Text.Json;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Infrastructure.Messaging;
using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Telemetry;

namespace Amolenk.Admitto.Worker;

/// <summary>
/// Receives cloud events from the Azure Storage queues and dispatches them to the appropriate handlers.
/// </summary>
public class MessageQueuesWorker(
    ServiceBusClient serviceBusClient,
    IServiceProvider serviceProvider,
    IOptions<MessageQueuesWorkerOptions> options,
    ILoggerFactory loggerFactory,
    ILogger<MessageQueuesWorker> logger)
    : BackgroundService
{
    private readonly AzureServiceBusQueueProcessor _queueProcessor = new(
        serviceBusClient,
        "queue",
        logger);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return CreateRetryStrategy("Queue").ExecuteAsync(
            ct => _queueProcessor.RunAsync(HandleMessageAsync, ct),
            stoppingToken).AsTask();
    }

    private async ValueTask HandleMessageAsync(CloudEvent cloudEvent, CancellationToken cancellationToken)
    {
        logger.LogDebug("Received message from queue: {MessageType}", cloudEvent.Type);

        var message = JsonSerializer.Deserialize(
            cloudEvent.Data!.ToString(),
            GetType(cloudEvent.Type),
            JsonSerializerOptions.Web);

        using var scope = serviceProvider.CreateScope();

        switch (message)
        {
            case Command command:
                await HandleCommandAsync(command, scope.ServiceProvider, cancellationToken);
                break;
            case DomainEvent domainEvent:
                await HandleDomainEventAsync(domainEvent, scope.ServiceProvider, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Cannot handle outbox message of type: {cloudEvent.Type}");
        }

        // Commit the unit of work for this message
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static Type GetType(string typeName)
    {
        if (typeName.EndsWith("Command"))
        {
            return Type.GetType(
                $"Amolenk.Admitto.Application.{typeName}, Admitto.Application",
                true)!;
        }

        if (typeName.EndsWith("DomainEvent"))
        {
            return Type.GetType(
                $"Amolenk.Admitto.Domain.{typeName}, Admitto.Domain",
                true)!;
        }

        if (typeName.EndsWith("ApplicationEvent"))
        {
            return Type.GetType(
                $"Amolenk.Admitto.Application.{typeName}, Admitto.Application",
                true)!;
        }

        throw new ArgumentException($"Unknown message type '{typeName}'", nameof(typeName));
    }

    private async ValueTask HandleCommandAsync(
        Command command,
        IServiceProvider scopedServiceProvider,
        CancellationToken cancellationToken)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        var handler = scopedServiceProvider.GetRequiredService(handlerType);

        logger.LogDebug(
            "Handling command {EventType} with handler {HandlerType}",
            command.GetType().Name,
            handler!.GetType().Name);

        await ((dynamic)handler).HandleAsync((dynamic)command, cancellationToken);
    }

    private async ValueTask HandleDomainEventAsync(
        DomainEvent domainEvent,
        IServiceProvider scopedServiceProvider,
        CancellationToken cancellationToken)
    {
        var handlerType = typeof(IEventualDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = scopedServiceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            logger.LogDebug(
                "Handling domain event {EventType} with handler {HandlerType}",
                domainEvent.GetType().Name,
                handler!.GetType().Name);

            await ((dynamic)handler).HandleAsync((dynamic)domainEvent, cancellationToken);
        }
    }

    private ResiliencePipeline CreateRetryStrategy(string instanceName)
    {
        var telemetryOptions = new TelemetryOptions
        {
            LoggerFactory = loggerFactory
        };

        var workerOptions = options.Value;

        return new ResiliencePipelineBuilder { Name = nameof(MessageQueuesWorker), InstanceName = instanceName }
            .AddRetry(
                new RetryStrategyOptions
                {
                    BackoffType = workerOptions.RetryBackoffType,
                    MaxDelay = workerOptions.MaxRetryDelay,
                    MaxRetryAttempts = workerOptions.MaxRetryAttempts
                })
            .ConfigureTelemetry(telemetryOptions)
            .Build();
    }
}