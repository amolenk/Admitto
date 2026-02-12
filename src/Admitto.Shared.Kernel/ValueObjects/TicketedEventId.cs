using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public readonly record struct TicketedEventId : IGuidValueObject
{
    public Guid Value { get; }
    
    private TicketedEventId(Guid value) => Value = value;

    public static TicketedEventId New() => new(Guid.NewGuid());

    public static ValidationResult<TicketedEventId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new TicketedEventId(v));

    public static TicketedEventId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new TicketedEventId(v)).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}
