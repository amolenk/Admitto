using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Domain.ValueObjects;

public readonly record struct EmailTemplateId : IGuidValueObject
{
    public Guid Value { get; }

    private EmailTemplateId(Guid value) => Value = value;

    public static EmailTemplateId New() => new(Guid.NewGuid());

    public static ValidationResult<EmailTemplateId> TryFrom(Guid value)
        => GuidValueObject.TryFrom(value, v => new EmailTemplateId(v));

    public static EmailTemplateId From(Guid value)
        => GuidValueObject.TryFrom(value, v => new EmailTemplateId(v)).GetValueOrThrow();

    public override string ToString() => Value.ToString();
}
