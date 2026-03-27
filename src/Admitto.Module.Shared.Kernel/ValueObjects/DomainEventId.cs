using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

public readonly record struct DomainEventId : IGuidValueObject
{
    public Guid Value { get; }
    
    private DomainEventId(Guid value) => Value = value;

    public static DomainEventId New() => new(Guid.NewGuid());

    public static ValidationResult<DomainEventId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new DomainEventId(v));

    public static DomainEventId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new DomainEventId(v)).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}