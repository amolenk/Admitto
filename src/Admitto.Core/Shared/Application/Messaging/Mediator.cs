using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Shared.Application.Messaging;

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

    /// <summary>
    /// Type-erased overload used by infrastructure (e.g., <see cref="QueueMessageDispatcher"/>)
    /// when only the <see cref="ICommand"/> interface type is known at compile time.
    /// </summary>
    ValueTask SendCommandAsync(
        ICommand command,
        CancellationToken cancellationToken = default);
}

public partial class Mediator(IServiceProvider serviceProvider, ILogger<Mediator> logger) : IMediator
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, ICommand, CancellationToken, ValueTask>>
        _commandDispatchers = new();

    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IDomainEvent, CancellationToken, ValueTask>>
        _domainEventDispatchers = new();

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
        var dispatcher = _domainEventDispatchers.GetOrAdd(eventType, static t =>
        {
            var method = typeof(Mediator)
                .GetMethod(nameof(DispatchDomainEventAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(t);
            return (Func<IServiceProvider, IDomainEvent, CancellationToken, ValueTask>)
                Delegate.CreateDelegate(
                    typeof(Func<IServiceProvider, IDomainEvent, CancellationToken, ValueTask>), method);
        });

        LogEventHandling(logger, eventType.FullName!);

        await HandleWithActivityAsync(
            "domain-event",
            eventType,
            ct => dispatcher(serviceProvider, domainEvent, ct),
            cancellationToken);
    }

    public ValueTask SendCommandAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        var commandType = command.GetType();
        var dispatcher = _commandDispatchers.GetOrAdd(commandType, static t =>
        {
            var method = typeof(Mediator)
                .GetMethod(nameof(DispatchCommandAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(t);
            return (Func<IServiceProvider, ICommand, CancellationToken, ValueTask>)
                Delegate.CreateDelegate(
                    typeof(Func<IServiceProvider, ICommand, CancellationToken, ValueTask>), method);
        });

        LogCommandHandling(logger, commandType.FullName!, commandType.FullName!);

        return HandleWithActivityAsync(
            "command",
            commandType,
            ct => dispatcher(serviceProvider, command, ct),
            cancellationToken);
    }

    private static ValueTask DispatchCommandAsync<TCommand>(
        IServiceProvider sp, ICommand cmd, CancellationToken ct)
        where TCommand : ICommand
    {
        var handler = sp.GetRequiredService<ICommandHandler<TCommand>>();
        return handler.HandleAsync((TCommand)cmd, ct);
    }

    private static async ValueTask DispatchDomainEventAsync<TDomainEvent>(
        IServiceProvider sp, IDomainEvent evt, CancellationToken ct)
        where TDomainEvent : IDomainEvent
    {
        var handlers = sp.GetServices<IDomainEventHandler<TDomainEvent>>();
        foreach (var handler in handlers)
            await handler.HandleAsync((TDomainEvent)evt, ct);
    }

    private static async ValueTask HandleWithActivityAsync(
        string kind,
        Type messageType,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken cancellationToken)
    {
        using var activity = StartHandlerActivity(kind, messageType, null);
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

    private static Activity? StartHandlerActivity(string kind, Type messageType, Type? handlerType)
    {
        var activity = AdmittoActivitySource.ActivitySource.StartActivity(
            $"{kind} {messageType.Name}",
            ActivityKind.Internal);

        activity?.AddTag("admitto.message.kind", kind);
        activity?.AddTag("admitto.message.type", messageType.FullName);
        if (handlerType is not null)
            activity?.AddTag("admitto.handler.type", handlerType.FullName);
        return activity;
    }

    [LoggerMessage(LogLevel.Information, "Handling command of type '{CommandType}' with handler '{handlerType}'")]
    static partial void LogCommandHandling(ILogger<Mediator> logger, string commandType, string handlerType);

    [LoggerMessage(LogLevel.Information, "Handling event of type '{EventType}'")]
    static partial void LogEventHandling(ILogger<Mediator> logger, string eventType);

    [LoggerMessage(LogLevel.Information, "Handling query of type '{QueryType}' with handler '{handlerType}'")]
    static partial void LogQueryHandling(ILogger<Mediator> logger, string queryType, string handlerType);
}