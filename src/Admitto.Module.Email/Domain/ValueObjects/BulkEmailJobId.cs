using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Domain.ValueObjects;

public readonly record struct BulkEmailJobId : IGuidValueObject
{
    public Guid Value { get; }

    private BulkEmailJobId(Guid value) => Value = value;

    public static BulkEmailJobId New() => new(Guid.NewGuid());

    public static ValidationResult<BulkEmailJobId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new BulkEmailJobId(v));

    public static BulkEmailJobId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new BulkEmailJobId(v)).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}
