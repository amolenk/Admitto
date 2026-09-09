using Vogen;

namespace Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

[ValueObject<Guid>]
public partial struct RegistrationCycleId
{
    public static RegistrationCycleId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("Registration cycle ID cannot be empty.");
}
