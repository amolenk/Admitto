namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public readonly record struct TicketTypeId(Guid Value) : IGuidValueObject
{
    public static TicketTypeId New() => new(Guid.NewGuid());
}
