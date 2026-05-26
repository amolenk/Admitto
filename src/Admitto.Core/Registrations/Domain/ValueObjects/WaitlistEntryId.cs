using Vogen;

namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

[ValueObject<Guid>]
public partial struct WaitlistEntryId
{
    public static WaitlistEntryId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("WaitlistEntry ID cannot be empty.");
}
