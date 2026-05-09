using Vogen;

namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

[ValueObject<Guid>]
public partial struct RegistrationId
{
    public static RegistrationId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("Registration ID cannot be empty.");
}
