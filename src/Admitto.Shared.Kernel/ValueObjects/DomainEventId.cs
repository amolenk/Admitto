using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public readonly record struct DomainEventId : IGuidValueObject
{
    public Guid Value { get; }
    
    private DomainEventId(Guid value) => Value = value;

    public static DomainEventId New() => new(Guid.NewGuid());

    public static ValidationResult<DomainEventId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new DomainEventId(v), Errors.Empty);

    public static DomainEventId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new DomainEventId(v), Errors.Empty).GetValueOrThrow();

    public override string ToString() => Value.ToString();

    private static class Errors
    {
        public static readonly Error Empty =
            new("domain_event_id.empty", "Domain event ID is required.");
    }
}