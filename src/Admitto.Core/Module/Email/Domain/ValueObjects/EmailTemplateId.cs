using Vogen;

namespace Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;

[ValueObject<Guid>]
public partial struct EmailTemplateId
{
    public static EmailTemplateId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("Email template ID cannot be empty.");
}

