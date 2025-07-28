namespace Amolenk.Admitto.Application.Common.Abstractions;

public record Command
{
    public Guid CommandId { get; set; } = Guid.NewGuid();
}