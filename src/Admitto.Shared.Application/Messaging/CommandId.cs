namespace Amolenk.Admitto.Shared.Application.Messaging;

public readonly record struct CommandId(Guid Value)
{
    public static CommandId New() => new(Guid.NewGuid());
}