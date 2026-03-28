using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

public readonly record struct TicketTypeId : IGuidValueObject
{
    public Guid Value { get; }
    
    private TicketTypeId(Guid value) => Value = value;

    public static TicketTypeId New() => new(Guid.NewGuid());

    public static ValidationResult<TicketTypeId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new TicketTypeId(v));

    public static TicketTypeId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new TicketTypeId(v)).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}
