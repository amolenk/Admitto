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

    ValueTask PublishDomainEventAsync<TDomainEvent>(
        TDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TDomainEvent : IDomainEvent;
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

        return handler.HandleAsync(command, cancellationToken);
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

        return handler.HandleAsync(command, cancellationToken);
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

        return handler.HandleAsync(query, cancellationToken);
    }

    public async ValueTask PublishDomainEventAsync<TDomainEvent>(
        TDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TDomainEvent : IDomainEvent
    {
        var handlers = serviceProvider
            .GetServices<IDomainEventHandler<TDomainEvent>>()
            .ToList();

        foreach (var handler in handlers)
        {
            LogEventHandling(logger, domainEvent.GetType().FullName!, handler.GetType().FullName!);

            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }

    [LoggerMessage(LogLevel.Information, "Handling command of type '{CommandType}' with handler '{handlerType}'")]
    static partial void LogCommandHandling(ILogger<Mediator> logger, string commandType, string handlerType);

    [LoggerMessage(LogLevel.Information, "Handling event of type '{EventType}' with handler '{handlerType}'")]
    static partial void LogEventHandling(ILogger<Mediator> logger, string eventType, string handlerType);

    [LoggerMessage(LogLevel.Information, "Handling query of type '{QueryType}' with handler '{handlerType}'")]
    static partial void LogQueryHandling(ILogger<Mediator> logger, string queryType, string handlerType);
}