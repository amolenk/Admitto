namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface ICommandHandler
{
}

public interface ICommandHandler<in TCommand> : ICommandHandler
    where TCommand : ICommand
{
    ValueTask HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResultValue> : ICommandHandler
    where TCommand : ICommand
{
    ValueTask<Result<TResultValue>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
