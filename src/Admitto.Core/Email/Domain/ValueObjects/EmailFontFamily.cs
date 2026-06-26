using Vogen;

namespace Amolenk.Admitto.Core.Email.Domain.ValueObjects;

/// <summary>
/// CSS font-family string. The backend stores the UI-selected value without font-safety validation.
/// </summary>
[ValueObject<string>]
public partial struct EmailFontFamily
{
    public const int MaxLength = 200;

    private static string NormalizeInput(string value) => value?.Trim() ?? string.Empty;

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Font family is required.");
        if (value.Length > MaxLength)
            return Validation.Invalid($"Font family must be at most {MaxLength} character(s).");
        return Validation.Ok;
    }
}
