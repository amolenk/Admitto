using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Amolenk.Admitto.Core.Shared.Application;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Azure.Messaging;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;

/// <summary>
/// Parses a CloudEvent pulled from the queue, restores the W3C trace context,
/// deserializes the payload to its CLR type and dispatches it to all registered handlers,
/// committing each handler's module unit of work after a successful invocation.
/// </summary>
internal sealed partial class QueueMessageDispatcher(
    IServiceProvider serviceProvider,
    ILogger<QueueMessageDispatcher> logger)
{
    // Keep a cache of handler invokers to avoid reflection overhead
    private static readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, ValueTask>>
        HandlerInvokers = new();

    /// <summary>
    /// Dispatches a CloudEvent to all registered handlers.
    /// </summary>
    public async ValueTask DispatchAsync(CloudEvent cloudEvent, CancellationToken cancellationToken)
    {
        // Restore the W3C trace context.
        var traceParent = cloudEvent.ExtensionAttributes.TryGetValue(
            AdmittoActivitySource.TraceParentAttribute,
            out var tp)
            ? tp as string
            : null;
        var traceState = cloudEvent.ExtensionAttributes.TryGetValue(
            AdmittoActivitySource.TraceStateAttribute,
            out var ts)
            ? ts as string
            : null;

        using var activity = AdmittoActivitySource.ActivitySource.StartActivity(
            $"queue receive {cloudEvent.Type}",
            ActivityKind.Consumer,
            traceParent ?? string.Empty);

        if (activity is not null && !string.IsNullOrEmpty(traceState))
        {
            activity.TraceStateString = traceState;
        }

        activity?.AddTag("admitto.message.id", cloudEvent.Id);
        activity?.AddTag("admitto.message.type", cloudEvent.Type);

        try
        {
            var message = GetMessageFromCloudEvent(cloudEvent);
            if (message is null)
            {
                LogUnknownMessageType(logger, cloudEvent.Type);

                // Don't crash the consumer on an unknown message — let it be deleted
                // so it doesn't poison the queue.
                return;
            }

            await DispatchToHandlersAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            throw;
        }
    }

    /// <summary>
    /// Dispatches a message (command or integration event) to all registered handlers.
    /// </summary>
    private async ValueTask DispatchToHandlersAsync(
        object message,
        CancellationToken cancellationToken)
    {
        // Resolve the handler interface type.
        var handlerOpenType = message is ICommand
            ? typeof(ICommandHandler<>)
            : typeof(IIntegrationEventHandler<>);

        // Make the generic interface type and get all handlers that implement it.
        var messageType = message.GetType();
        var messageTypeName = messageType.FullName!;
        var handlerInterfaceType = handlerOpenType.MakeGenericType(messageType);
        var handlers = serviceProvider.GetServices(handlerInterfaceType).ToList();

        if (handlers.Count == 0)
        {
            LogNoHandlersFound(logger, messageTypeName);
            return;
        }

        // Get or add an invoker function for this type of handler interface.
        // An invoker function is a lambda that invokes the handler's HandleAsync method using reflection.
        // The invokers are cached to avoid reflection overhead.
        var invoker = HandlerInvokers.GetOrAdd(
            handlerInterfaceType,
            static t =>
            {
                var method = t.GetMethod("HandleAsync")!;
                return (handler, msg, ct) => (ValueTask)method.Invoke(handler, [msg, ct])!;
            });

        foreach (var handler in handlers)
        {
            if (handler is null) continue;

            using var handlerActivity = AdmittoActivitySource.ActivitySource.StartActivity(
                $"handler {handler.GetType().FullName}");

            handlerActivity?.AddTag("admitto.handler.type", handler.GetType().FullName);
            handlerActivity?.AddTag("admitto.message.type", messageTypeName);

            try
            {
                await invoker(handler, message, cancellationToken);

                // We need the module key of the handler to resolve its unit of work.
                // Get it from the type name by convention: Amolenk.Admitto.Core.<ModuleKey>...
                var moduleKey = GetModuleKey(handler.GetType());

                // Commit the module's unit of work.
                var unitOfWork = serviceProvider.GetRequiredKeyedService<IUnitOfWork>(moduleKey);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (BusinessRuleViolationException ex)
            {
                // No use re-throwing, the retry will also fail.
                handlerActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                handlerActivity?.AddTag("exception.type", ex.GetType().FullName);

                LogBusinessRuleViolation(
                    messageTypeName,
                    handler.GetType().FullName!,
                    ex.Error,
                    ex);
            }
            catch (Exception ex)
            {
                handlerActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                handlerActivity?.AddTag("exception.type", ex.GetType().FullName);
                throw;
            }
        }
    }

    /// <summary>
    /// Extracts and deserializes the payload of a CloudEvent into its corresponding CLR object type.
    /// </summary>
    private static object? GetMessageFromCloudEvent(CloudEvent cloudEvent)
    {
        // Resolve the CLR message type from the cloud event type.
        var clrType = GetClrType(cloudEvent.Type);
        if (clrType is null) return null;

        var payload = cloudEvent.Data?.ToString() ?? "{}";
        return JsonSerializer.Deserialize(payload, clrType, JsonSerializerOptions.Web);
    }

    /// <summary>
    /// Resolves the CLR type of message from its cloud event type.
    /// </summary>
    private static Type? GetClrType(string cloudEventType)
    {
        var parts = cloudEventType.Split(':');
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid message type: {cloudEventType}");

        var moduleKey = parts[0];
        var shortTypeName = parts[1];

        return Type.GetType(
            shortTypeName.EndsWith("Command")
                ? $"Amolenk.Admitto.Core.{moduleKey}.Application.UseCases.{shortTypeName}"
                : $"Amolenk.Admitto.Core.{moduleKey}.Contracts.IntegrationEvents.{shortTypeName}");
    }

    /// <summary>
    /// Extracts the module key from a type's namespace using the project's
    /// <c>Amolenk.Admitto.Core.&lt;Module&gt;</c> convention.
    /// </summary>
    private static string GetModuleKey(Type type)
    {
        var ns = type.Namespace ?? throw new InvalidOperationException($"Type {type.FullName} has no namespace.");

        var parts = ns.Split('.');
        if (parts.Length >= 4 && parts[0] == "Amolenk" && parts[1] == "Admitto" && parts[2] == "Core")
            return parts[3];

        throw new InvalidOperationException(
            $"Type {type.FullName} does not follow the expected module namespace convention.");
    }

    [LoggerMessage(
        LogLevel.Warning,
        "Received message of unknown type '{MessageType}'; discarding.")]
    static partial void LogUnknownMessageType(ILogger<QueueMessageDispatcher> logger, string messageType);

    [LoggerMessage(
        LogLevel.Warning,
        "No handlers registered for message type '{MessageType}'; discarding.")]
    static partial void LogNoHandlersFound(ILogger<QueueMessageDispatcher> logger, string messageType);

    [LoggerMessage(
        LogLevel.Error,
        "Business rule violation while handling message of type {MessageType} with handler {HandlerType}. " +
        "This likely indicates a data issue that needs to be resolved manually; the message will be discarded. " +
        "Error details: {ErrorDetails}")]
    partial void LogBusinessRuleViolation(
        string messageType,
        string handlerType,
        Error errorDetails,
        BusinessRuleViolationException businessRuleViolationException);
}
