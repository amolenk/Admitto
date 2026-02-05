namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public readonly record struct DomainEventId(Guid Value)
{
    public static DomainEventId New() => new(Guid.NewGuid());
}