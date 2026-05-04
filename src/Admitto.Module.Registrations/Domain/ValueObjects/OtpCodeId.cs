using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

public readonly record struct OtpCodeId : IGuidValueObject
{
    public Guid Value { get; }

    private OtpCodeId(Guid value) => Value = value;

    public static OtpCodeId New() => new(Guid.NewGuid());

    public static ValidationResult<OtpCodeId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new OtpCodeId(v));

    public static OtpCodeId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new OtpCodeId(v)).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}
