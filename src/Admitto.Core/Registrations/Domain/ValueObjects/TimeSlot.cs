using Vogen;

namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

[ValueObject<string>]
public partial struct TimeSlot
{
    public const int MaxLength = 64;

    private static string NormalizeInput(string value) => value.Trim();

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Time slot is required.");
        if (value.Length > MaxLength)
            return Validation.Invalid($"Time slot must be at most {MaxLength} character(s).");
        return Validation.Ok;
    }
}
