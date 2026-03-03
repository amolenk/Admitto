namespace Amolenk.Admitto.Shared.Application.Messaging;

public interface ICommand
{
    Guid CommandId { get; }
}

public interface ICommand<TResultValue> : ICommand;

public record Command : ICommand
{
    // Properties must be init-settable for deserialization.
    public Guid CommandId { get; init; } = Guid.NewGuid();
}

public record Command<TResultValue> : Command, ICommand<TResultValue>;
