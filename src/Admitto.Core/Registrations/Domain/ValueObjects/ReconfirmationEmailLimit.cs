using Vogen;

namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

[ValueObject<int>]
public partial struct ReconfirmationEmailLimit
{
    public const int MinValue = 1;

    private static Validation Validate(int value)
        => value >= MinValue
            ? Validation.Ok
            : Validation.Invalid("Maximum reconfirmation emails must be at least 1.");
}
