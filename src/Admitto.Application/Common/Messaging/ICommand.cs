namespace Amolenk.Admitto.Application.Common.Messaging;

public record Command
{
    // Properties must be init-settable for deserialization.

    public Guid CommandId { get; init; } = Guid.NewGuid();
}