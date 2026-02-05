using Amolenk.Admitto.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Shared.Application.Messaging;

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

internal class Mediator(IServiceProvider serviceProvider) : IMediator
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

        return handler.HandleAsync(query, cancellationToken);
    }

    public ValueTask PublishDomainEventAsync<TDomainEvent>(
        TDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TDomainEvent : IDomainEvent
    {
        var handler = serviceProvider.GetRequiredService<IDomainEventHandler<TDomainEvent>>();
        if (handler is null)
        {
            throw new InvalidOperationException(
                $"No handler registered for domain event of type '{domainEvent.GetType().FullName}'");
        }

        return handler.HandleAsync(domainEvent, cancellationToken);
    }
}