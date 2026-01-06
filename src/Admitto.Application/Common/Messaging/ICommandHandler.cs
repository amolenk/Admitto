namespace Amolenk.Admitto.Application.Common.Messaging;

public interface IApiCommandHandler
{
}

public interface IApiCommandHandler<in TCommand> : IApiCommandHandler
    where TCommand : Command
{
    ValueTask HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IWorkerCommandHandler
{
}

public interface IWorkerCommandHandler<in TCommand> : IWorkerCommandHandler
    where TCommand : Command
{
    ValueTask HandleAsync(TCommand command, CancellationToken cancellationToken);
}
