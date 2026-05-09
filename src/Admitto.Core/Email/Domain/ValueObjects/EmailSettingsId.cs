using Vogen;

namespace Amolenk.Admitto.Core.Email.Domain.ValueObjects;

[ValueObject<Guid>]
public partial struct EmailSettingsId
{
    public static EmailSettingsId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("Email settings ID cannot be empty.");
}

