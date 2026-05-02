using System.Diagnostics;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Module.Shared.Application.Messaging;

public interface IMediator
{
    ValueTask SendAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    ValueTask<TResult> SendReceiveAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>;

    ValueTask<TResult> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>;

    /// <summary>
    /// Type-erased overload used by infrastructure (e.g., <see cref="DomainEventsInterceptor"/>)
    /// when only the <see cref="IDomainEvent"/> interface type is known at compile time.
    /// </summary>
    ValueTask PublishDomainEventAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default);
}

public partial class Mediator(IServiceProvider serviceProvider, ILogger<Mediator> logger) : IMediator
{
    public ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        var handler = serviceProvider.GetService<ICommandHandler<TCommand>>();
        if (handler is null)
        {
            throw new InvalidOperationException(
                $"No handler registered for command of type '{command.GetType().FullName}'");
        }

        LogCommandHandling(logger, command.GetType().FullName!, handler.GetType().FullName!);

        return HandleWithActivityAsync(
            "command",
            command.GetType(),
            handler.GetType(),
            ct => handler.HandleAsync(command, ct),
            cancellationToken);
    }

    public ValueTask<TResult> SendReceiveAsync<TCommand, TResult>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        var handler = serviceProvider.GetService<ICommandHandler<TCommand, TResult>>();
        if (handler is null)
        {
            throw new InvalidOperationException(
                $"No handler registered for command of type '{command.GetType().FullName}'");
        }

        LogCommandHandling(logger, command.GetType().FullName!, handler.GetType().FullName!);

        return HandleWithActivityAsync(
            "command",
            command.GetType(),
            handler.GetType(),
            ct => handler.HandleAsync(command, ct),
            cancellationToken);
    }

    public ValueTask<TResult> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        var handler = serviceProvider.GetService<IQueryHandler<TQuery, TResult>>();
        if (handler is null)
        {
            throw new InvalidOperationException(
                $"No handler registered for query of type '{query.GetType().FullName}'");
        }

        LogQueryHandling(logger, query.GetType().FullName!, handler.GetType().FullName!);

        return HandleWithActivityAsync(
            "query",
            query.GetType(),
            handler.GetType(),
            ct => handler.HandleAsync(query, ct),
            cancellationToken);
    }

    public async ValueTask PublishDomainEventAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handlers = serviceProvider.GetServices(handlerType).Cast<IDomainEventHandler>().ToList();

        foreach (var handler in handlers)
        {
            LogEventHandling(logger, eventType.FullName!, handler.GetType().FullName!);

            await HandleWithActivityAsync(
                "domain-event",
                eventType,
                handler.GetType(),
                ct => handler.HandleAsync(domainEvent, ct),
                cancellationToken);
        }
    }

    private static async ValueTask HandleWithActivityAsync(
        string kind,
        Type messageType,
        Type handlerType,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken cancellationToken)
    {
        using var activity = StartHandlerActivity(kind, messageType, handlerType);
        try
        {
            await handler(cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            throw;
        }
    }

    private static async ValueTask<TResult> HandleWithActivityAsync<TResult>(
        string kind,
        Type messageType,
        Type handlerType,
        Func<CancellationToken, ValueTask<TResult>> handler,
        CancellationToken cancellationToken)
    {
        using var activity = StartHandlerActivity(kind, messageType, handlerType);
        try
        {
            return await handler(cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            throw;
        }
    }

    private static Activity? StartHandlerActivity(string kind, Type messageType, Type handlerType)
    {
        var activity = AdmittoActivitySource.ActivitySource.StartActivity(
            $"{kind} {messageType.Name}",
            ActivityKind.Internal);

        activity?.AddTag("admitto.message.kind", kind);
        activity?.AddTag("admitto.message.type", messageType.FullName);
        activity?.AddTag("admitto.handler.type", handlerType.FullName);
        return activity;
    }

    [LoggerMessage(LogLevel.Information, "Handling command of type '{CommandType}' with handler '{handlerType}'")]
    static partial void LogCommandHandling(ILogger<Mediator> logger, string commandType, string handlerType);

    [LoggerMessage(LogLevel.Information, "Handling event of type '{EventType}' with handler '{handlerType}'")]
    static partial void LogEventHandling(ILogger<Mediator> logger, string eventType, string handlerType);

    [LoggerMessage(LogLevel.Information, "Handling query of type '{QueryType}' with handler '{handlerType}'")]
    static partial void LogQueryHandling(ILogger<Mediator> logger, string queryType, string handlerType);
}