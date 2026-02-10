using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Shared.Application.Messaging;

public readonly record struct CommandId : IGuidValueObject
{
    public Guid Value { get; }
    
    private CommandId(Guid value) => Value = value;

    public static CommandId New() => new(Guid.NewGuid());

    public static ValidationResult<CommandId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new CommandId(v));

    public static CommandId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new CommandId(v)).GetValueOrThrow();

    public static CommandId For<TCommand>(DomainEventId domainEventId)
        where TCommand : ICommand
        => From(DeterministicGuid.Create($"{domainEventId}:{typeof(TCommand).FullName}"));

    public override string ToString() => Value.ToString();
}
