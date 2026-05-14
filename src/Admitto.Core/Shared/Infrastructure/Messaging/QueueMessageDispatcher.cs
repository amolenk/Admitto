using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Amolenk.Admitto.Core.Shared.Application;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Contracts;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Azure.Messaging;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;

/// <summary>
/// Parses a CloudEvent pulled from the Azure Storage Queue, restores the
/// W3C trace context written by <c>OutboxMessageSender</c>, deserializes the
/// payload to its CLR type and dispatches it to all registered handlers,
/// committing each handler's module unit of work after a successful invocation.
/// </summary>
internal sealed partial class QueueMessageDispatcher(
    MessageTypeRegistry registry,
    IServiceProvider serviceProvider,
    ILogger<QueueMessageDispatcher> logger)
{
    private static readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, ValueTask>>
        _handlerInvokers = new();

    public async ValueTask DispatchAsync(CloudEvent cloudEvent, CancellationToken cancellationToken)
    {
        if (!registry.TryResolve(cloudEvent.Type, out var entry))
        {
            LogUnknownMessageType(logger, cloudEvent.Type);
            // Don't crash the consumer on an unknown message — let it be deleted
            // so it doesn't poison the queue. Persisting to a dead-letter store
            // is a follow-up.
            return;
        }

        var traceParent = cloudEvent.ExtensionAttributes.TryGetValue(
            AdmittoActivitySource.TraceParentAttribute,
            out var tp) ? tp as string : null;
        var traceState = cloudEvent.ExtensionAttributes.TryGetValue(
            AdmittoActivitySource.TraceStateAttribute,
            out var ts) ? ts as string : null;

        using var activity = AdmittoActivitySource.ActivitySource.StartActivity(
            $"queue receive {cloudEvent.Type}",
            ActivityKind.Consumer,
            traceParent ?? string.Empty);
        if (activity is not null && !string.IsNullOrEmpty(traceState))
        {
            activity.TraceStateString = traceState;
        }
        activity?.AddTag("admitto.message.type", cloudEvent.Type);
        activity?.AddTag("admitto.message.id", cloudEvent.Id);
        activity?.AddTag("admitto.module.name", entry.ModuleName);

        var payload = cloudEvent.Data?.ToString() ?? "{}";

        try
        {
            var message = JsonSerializer.Deserialize(payload, entry.ClrType, JsonSerializerOptions.Web)!;
            await DispatchToHandlersAsync(message, entry, cloudEvent.Type, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            throw;
        }
    }

    private async ValueTask DispatchToHandlersAsync(
        object message,
        MessageTypeRegistry.Entry entry,
        string messageType,
        CancellationToken cancellationToken)
    {
        var handlerOpenType = entry.Kind == MessageTypeRegistry.MessageKind.Command
            ? typeof(ICommandHandler<>)
            : typeof(IIntegrationEventHandler<>);

        var handlerInterfaceType = handlerOpenType.MakeGenericType(entry.ClrType);
        var handlers = serviceProvider.GetServices(handlerInterfaceType).ToList();

        if (handlers.Count == 0)
        {
            LogNoHandlersFound(logger, messageType);
            return;
        }

        var invoker = _handlerInvokers.GetOrAdd(handlerInterfaceType, static t =>
        {
            var method = t.GetMethod("HandleAsync")!;
            return (handler, msg, ct) => (ValueTask)method.Invoke(handler, [msg, ct])!;
        });

        var kindLabel = entry.Kind == MessageTypeRegistry.MessageKind.Command ? "command" : "integration-event";

        foreach (var handler in handlers)
        {
            var moduleKey = MessageTypeRegistry.GetModuleKey(handler!.GetType());

            using var handlerActivity = AdmittoActivitySource.ActivitySource.StartActivity(
                $"{kindLabel} {entry.ClrType.Name}",
                ActivityKind.Internal);
            handlerActivity?.AddTag("admitto.message.kind", kindLabel);
            handlerActivity?.AddTag("admitto.message.type", entry.ClrType.FullName);
            handlerActivity?.AddTag("admitto.handler.type", handler.GetType().FullName);
            handlerActivity?.AddTag("admitto.module.key", moduleKey);

            try
            {
                await invoker(handler, message, cancellationToken);

                var unitOfWork = serviceProvider.GetRequiredKeyedService<IUnitOfWork>(moduleKey);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (BusinessRuleViolationException ex)
            {
                // No use re-throwing, the retry will also fail.
                handlerActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                handlerActivity?.AddTag("exception.type", ex.GetType().FullName);

                logger.LogError(ex, "Business rule violation while handling {MessageKind} of type {MessageType} with handler {HandlerType}. " +
                    "This likely indicates a data issue that needs to be resolved manually; the message will be discarded. " +
                    "Error details: {ErrorDetails}",
                    kindLabel, entry.ClrType.FullName, handler.GetType().FullName, ex.Error);
            }
            catch (Exception ex)
            {
                handlerActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                handlerActivity?.AddTag("exception.type", ex.GetType().FullName);
                throw;
            }
        }
    }

    [LoggerMessage(
        LogLevel.Warning,
        "Received message of unknown type '{MessageType}'; discarding.")]
    static partial void LogUnknownMessageType(ILogger<QueueMessageDispatcher> logger, string messageType);

    [LoggerMessage(
        LogLevel.Warning,
        "No handlers registered for message type '{MessageType}'; discarding.")]
    static partial void LogNoHandlersFound(ILogger<QueueMessageDispatcher> logger, string messageType);
}
