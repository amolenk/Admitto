using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Domain.ValueObjects;

public readonly record struct EmailLogId : IGuidValueObject
{
    public Guid Value { get; }

    private EmailLogId(Guid value) => Value = value;

    public static EmailLogId New() => new(Guid.NewGuid());

    public static ValidationResult<EmailLogId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new EmailLogId(v));

    public static EmailLogId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new EmailLogId(v)).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}
