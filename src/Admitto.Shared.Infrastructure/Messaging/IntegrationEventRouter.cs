using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Application.Persistence;
using Amolenk.Admitto.Shared.Contracts;

namespace Amolenk.Admitto.Shared.Infrastructure.Messaging;

internal partial class IntegrationEventRouter(IServiceProvider serviceProvider, ILogger<Mediator> logger)
{
    public async ValueTask DispatchIntegrationEventAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent
    {
        var subscriberModules = serviceProvider.GetServices<IntegrationEventSubscriber>()
            .ToList();

        foreach (var subscriberModule in subscriberModules)
        {
            using var scope = serviceProvider.CreateScope();

            var handlers = serviceProvider
                .GetKeyedServices<IIntegrationEventHandler<TIntegrationEvent>>(subscriberModule.ModuleKey)
                .ToList();

            foreach (var handler in handlers)
            {
                LogEventHandling(logger, integrationEvent.GetType().FullName!, handler.GetType().FullName!);

                await handler.HandleAsync(integrationEvent, cancellationToken);
            }

            var unitOfWork = scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(subscriberModule.ModuleKey);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    [LoggerMessage(
        LogLevel.Information,
        "Handling integration event of type '{EventType}' with handler '{handlerType}'")]
    static partial void LogEventHandling(ILogger<Mediator> logger, string eventType, string handlerType);
}