using Vogen;

namespace Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;

[ValueObject<Guid>]
public partial struct EmailLogId
{
    public static EmailLogId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("Email log ID cannot be empty.");
}

