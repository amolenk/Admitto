using Vogen;

namespace Amolenk.Admitto.Core.Email.Domain.ValueObjects;

/// <summary>
/// CSS color string used as the accent color in built-in emails.
/// </summary>
[ValueObject<string>]
public partial struct EmailAccentColor
{
    public const int MaxLength = 32;

    private static string NormalizeInput(string value) => value?.Trim() ?? string.Empty;

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Accent color is required.");
        if (value.Length > MaxLength)
            return Validation.Invalid($"Accent color must be at most {MaxLength} character(s).");
        return Validation.Ok;
    }
}
