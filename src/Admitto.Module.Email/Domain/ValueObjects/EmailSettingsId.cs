using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Domain.ValueObjects;

public readonly record struct EmailSettingsId : IGuidValueObject
{
    public Guid Value { get; }

    private EmailSettingsId(Guid value) => Value = value;

    public static EmailSettingsId New() => new(Guid.NewGuid());

    public static ValidationResult<EmailSettingsId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new EmailSettingsId(v));

    public static EmailSettingsId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new EmailSettingsId(v)).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}
