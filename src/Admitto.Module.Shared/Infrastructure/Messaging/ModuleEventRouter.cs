using System.Diagnostics;
using Amolenk.Admitto.Module.Shared.Application;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Messaging;

/// <summary>
/// Routes a deserialized module event to the producing module's handlers
/// (module events are intra-module, so we resolve the unit of work for the module
/// the event originated from and commit it after handling).
/// </summary>
internal sealed partial class ModuleEventRouter(IServiceProvider serviceProvider, ILogger<ModuleEventRouter> logger)
{
    public async ValueTask DispatchAsync(
        IModuleEvent moduleEvent,
        string moduleKey,
        CancellationToken cancellationToken = default)
    {
        var eventType = moduleEvent.GetType();
        var handlerInterfaceType = typeof(IModuleEventHandler<>).MakeGenericType(eventType);

        using var scope = serviceProvider.CreateScope();

        var handlers = scope.ServiceProvider
            .GetServices(handlerInterfaceType)
            .Cast<object>()
            .ToList();

        if (handlers.Count == 0) return;

        foreach (var handler in handlers)
        {
            LogEventHandling(logger, eventType.FullName!, handler.GetType().FullName!);

            using var activity = AdmittoActivitySource.ActivitySource.StartActivity(
                $"module-event {eventType.Name}",
                ActivityKind.Internal);
            activity?.AddTag("admitto.message.kind", "module-event");
            activity?.AddTag("admitto.message.type", eventType.FullName);
            activity?.AddTag("admitto.handler.type", handler.GetType().FullName);
            activity?.AddTag("admitto.module.key", moduleKey);

            try
            {
                var task = (ValueTask)handlerInterfaceType
                    .GetMethod(nameof(IModuleEventHandler<IModuleEvent>.HandleAsync))!
                    .Invoke(handler, [moduleEvent, cancellationToken])!;
                await task;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddTag("exception.type", ex.GetType().FullName);
                throw;
            }
        }

        var unitOfWork = scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(moduleKey);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(
        LogLevel.Information,
        "Handling module event of type '{EventType}' with handler '{HandlerType}'")]
    static partial void LogEventHandling(ILogger<ModuleEventRouter> logger, string eventType, string handlerType);
}
