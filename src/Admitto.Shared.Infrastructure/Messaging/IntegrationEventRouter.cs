using Amolenk.Admitto.Shared.Application.Persistence;
using Amolenk.Admitto.Shared.Contracts;
using Amolenk.Admitto.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Shared.Application.Messaging;

internal partial class IntegrationEventRouter(IServiceProvider serviceProvider, ILogger<Mediator> logger)
{
    public async ValueTask DispatchIntegrationEventAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent
    {
        var routes = serviceProvider.GetServices<IntegrationEventRoute>()
            .ToList();

        foreach (var route in routes)
        {
            using var scope = serviceProvider.CreateScope();

            var handlers = serviceProvider
                .GetKeyedServices<IIntegrationEventHandler<TIntegrationEvent>>(route.ModuleKey)
                .ToList();

            foreach (var handler in handlers)
            {
                LogEventHandling(logger, integrationEvent.GetType().FullName!, handler.GetType().FullName!);

                await handler.HandleAsync(integrationEvent, cancellationToken);
            }

            var unitOfWork = scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(route.ModuleKey);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    [LoggerMessage(
        LogLevel.Information,
        "Handling integration event of type '{EventType}' with handler '{handlerType}'")]
    static partial void LogEventHandling(ILogger<Mediator> logger, string eventType, string handlerType);
}

