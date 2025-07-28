namespace Amolenk.Admitto.Application.Common.Abstractions;

public record Command
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
}