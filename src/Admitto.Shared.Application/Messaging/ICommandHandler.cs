namespace Amolenk.Admitto.Shared.Application.Messaging;

public interface ICommandHandler
{
}

public interface ICommandHandler<in TCommand> : ICommandHandler
    where TCommand : ICommand
{
    ValueTask HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResult> : ICommandHandler
    where TCommand : ICommand<TResult>
{
    ValueTask<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
