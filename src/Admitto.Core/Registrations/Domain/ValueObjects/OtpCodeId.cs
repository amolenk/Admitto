using Vogen;

namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

[ValueObject<Guid>]
public partial struct OtpCodeId
{
    public static OtpCodeId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("OTP code ID cannot be empty.");
}

