using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

public readonly record struct ActivityLogId : IGuidValueObject
{
    public Guid Value { get; }

    private ActivityLogId(Guid value) => Value = value;

    public static ActivityLogId New() => new(Guid.NewGuid());

    public static ValidationResult<ActivityLogId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new ActivityLogId(v));

    public static ActivityLogId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new ActivityLogId(v)).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}
