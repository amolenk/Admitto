namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public readonly record struct TicketedEventId(Guid Value) : IGuidValueObject
{
    public static TicketedEventId New() => new(Guid.NewGuid());
}