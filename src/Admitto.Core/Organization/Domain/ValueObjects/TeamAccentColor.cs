using Vogen;

namespace Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

[ValueObject<string>]
public partial struct TeamAccentColor
{
    public const string Default = "#2563eb";
    public const int MaxLength = 7;

    private static string NormalizeInput(string value) => value.Trim().ToLowerInvariant();

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Team accent color is required.");
        if (value.Length != MaxLength || value[0] != '#' || !value[1..].All(Uri.IsHexDigit))
            return Validation.Invalid("Team accent color must be a hex color like #0f766e.");
        return Validation.Ok;
    }
}
