namespace Amolenk.Admitto.Shared.Application.Messaging;

public interface ICommand
{
    CommandId CommandId { get; }
}

public interface ICommand<TResultValue> : ICommand;

public record Command : ICommand
{
    // Properties must be init-settable for deserialization.
    public CommandId CommandId { get; init; } = CommandId.New();
}

public record Command<TResultValue> : Command, ICommand<TResultValue>;
