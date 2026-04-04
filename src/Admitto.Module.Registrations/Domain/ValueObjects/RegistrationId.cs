using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

public readonly record struct RegistrationId : IGuidValueObject
{
    public Guid Value { get; }

    private RegistrationId(Guid value) => Value = value;

    public static RegistrationId New() => new(Guid.NewGuid());

    public static ValidationResult<RegistrationId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new RegistrationId(v));

    public static RegistrationId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new RegistrationId(v)).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}