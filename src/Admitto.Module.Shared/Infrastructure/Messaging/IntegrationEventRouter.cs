using System.Diagnostics;
using Amolenk.Admitto.Module.Shared.Application;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Contracts;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Messaging;

/// <summary>
/// Routes a deserialized integration event to every subscribed module's handlers.
/// Each subscriber gets its own DI scope so its handlers can mutate that module's
/// DbContext and have <see cref="IUnitOfWork"/> committed at the end.
/// </summary>
internal sealed partial class IntegrationEventRouter(IServiceProvider serviceProvider, ILogger<IntegrationEventRouter> logger)
{
    public async ValueTask DispatchAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var eventType = integrationEvent.GetType();
        var handlerInterfaceType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

        var subscriberModules = serviceProvider.GetServices<IntegrationEventSubscriber>().ToList();

        foreach (var subscriberModule in subscriberModules)
        {
            using var scope = serviceProvider.CreateScope();

            var handlers = scope.ServiceProvider
                .GetKeyedServices(handlerInterfaceType, subscriberModule.ModuleKey)
                .Cast<object>()
                .ToList();

            if (handlers.Count == 0) continue;

            foreach (var handler in handlers)
            {
                LogEventHandling(logger, eventType.FullName!, handler.GetType().FullName!);

                using var activity = AdmittoActivitySource.ActivitySource.StartActivity(
                    $"integration-event {eventType.Name}",
                    ActivityKind.Internal);
                activity?.AddTag("admitto.message.kind", "integration-event");
                activity?.AddTag("admitto.message.type", eventType.FullName);
                activity?.AddTag("admitto.handler.type", handler.GetType().FullName);
                activity?.AddTag("admitto.module.key", subscriberModule.ModuleKey);

                try
                {
                    var task = (ValueTask)handlerInterfaceType
                        .GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!
                        .Invoke(handler, [integrationEvent, cancellationToken])!;
                    await task;
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity?.AddTag("exception.type", ex.GetType().FullName);
                    throw;
                }
            }

            var unitOfWork = scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(subscriberModule.ModuleKey);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    [LoggerMessage(
        LogLevel.Information,
        "Handling integration event of type '{EventType}' with handler '{HandlerType}'")]
    static partial void LogEventHandling(ILogger<IntegrationEventRouter> logger, string eventType, string handlerType);
}
