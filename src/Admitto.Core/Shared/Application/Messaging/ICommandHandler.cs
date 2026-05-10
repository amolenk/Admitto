namespace Amolenk.Admitto.Core.Shared.Application.Messaging;

/// <summary>
/// Non-generic base — enables type-erased dispatch from infrastructure (e.g., <see cref="QueueMessageDispatcher"/>)
/// without reflection or dynamic.
/// </summary>
public interface ICommandHandler
{
    ValueTask HandleAsync(ICommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand> : ICommandHandler
    where TCommand : ICommand
{
    ValueTask HandleAsync(TCommand command, CancellationToken cancellationToken);

    // Bridge the non-generic interface to the typed overload.
    ValueTask ICommandHandler.HandleAsync(ICommand command, CancellationToken cancellationToken)
        => HandleAsync((TCommand)command, cancellationToken);
}

public interface ICommandHandler<in TCommand, TResult> : ICommandHandler
    where TCommand : ICommand<TResult>
{
    ValueTask<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);

    // Bridge the non-generic interface (result is discarded for type-erased dispatch).
    ValueTask ICommandHandler.HandleAsync(ICommand command, CancellationToken cancellationToken)
        => new(HandleAsync((TCommand)command, cancellationToken).AsTask());
}
