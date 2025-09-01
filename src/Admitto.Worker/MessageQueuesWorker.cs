using System.Text.Json;
using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.Common.Core;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Infrastructure.Messaging;
using Amolenk.Admitto.Infrastructure.Persistence;
using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
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
                await ExecuteHandlerAsync(
                    command.CommandId,
                    command,
                    typeof(ICommandHandler<>),
                    scope.ServiceProvider,
                    cancellationToken);
                break;
            case DomainEvent domainEvent:
                await ExecuteHandlerAsync(
                    domainEvent.DomainEventId,
                    domainEvent,
                    typeof(IEventualDomainEventHandler<>),
                    scope.ServiceProvider,
                    cancellationToken);
                break;
            case ApplicationEvent applicationEvent:
                await ExecuteHandlerAsync(
                    applicationEvent.ApplicationEventId,
                    applicationEvent,
                    typeof(IApplicationEventHandler<>),
                    scope.ServiceProvider,
                    cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Cannot handle outbox message of type: {cloudEvent.Type}");
        }
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

    private async ValueTask ExecuteHandlerAsync(
        Guid messageId,
        object message,
        Type genericHandlerType,
        IServiceProvider scopedServiceProvider,
        CancellationToken cancellationToken)
    {
        var handlerType = genericHandlerType.MakeGenericType(message.GetType());
        var handlers = scopedServiceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var messageType = message.GetType().Name;
            var actualHandlerType = handler!.GetType().Name;

            var messageLogId = DeterministicGuid.Create($"{messageId}:{actualHandlerType}");

            // ReSharper disable once ExplicitCallerInfoArgument
            using var activity = AdmittoActivitySource.ActivitySource.StartActivity("handle message");
            activity?.SetTag("admitto.message.id", messageId);
            activity?.SetTag("admitto.message.type", messageType);
            activity?.SetTag("admitto.handler.type", actualHandlerType);
            activity?.SetTag("admitto.message_log.id", messageLogId);

            var context = scopedServiceProvider.GetRequiredService<ApplicationContext>();
            var messageProcessed = await context.MessageLogs.AsNoTracking().AnyAsync(
                l => l.Id == messageLogId,
                cancellationToken: cancellationToken);

            if (messageProcessed)
            {
                logger.LogInformation(
                    "Message of type '{MessageType}' with ID '{MessageId}' has already been processed by handler '{HandlerType}', skipping",
                    messageType,
                    messageId,
                    actualHandlerType);
                continue;
            }

            logger.LogDebug(
                "Handling '{MessageType}' with handler '{HandlerType}'",
                messageType,
                actualHandlerType);

            await ((dynamic)handler).HandleAsync((dynamic)message, cancellationToken);

            context.MessageLogs.Add(
                new MessageLog
                {
                    Id = messageLogId,
                    MessageId = messageId,
                    MessageType = messageType,
                    HandlerType = actualHandlerType,
                    ProcessedAt = DateTimeOffset.UtcNow
                });

            // Commit the unit of work for this message.
            var unitOfWork = scopedServiceProvider.GetRequiredService<IUnitOfWork>();
            await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

            // Clear the unit of work to avoid side effects between handlers of the same message.
            unitOfWork.Clear();
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