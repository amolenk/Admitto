namespace Amolenk.Admitto.Application.Common.Messaging;

public interface ICommandHandler
{
}

public interface ICommandHandler<in TCommand> : ICommandHandler
    where TCommand : Command
{
    ValueTask HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResult> : ICommandHandler
    where TCommand : Command
{
    ValueTask<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
