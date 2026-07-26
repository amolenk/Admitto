using Vogen;

namespace Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

/// <summary>
/// Hex accent color used for team branding. Owned by <c>Team</c> in the Organization
/// module and projected into the Email module's team context, where it is interpolated
/// directly into template <c>style</c> attributes — hence the strict format.
/// </summary>
[ValueObject<string>]
public partial struct AccentColor
{
    public const string Default = "#2563eb";
    public const int MaxLength = 7;

    private static string NormalizeInput(string value) => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Accent color is required.");
        if (value.Length != MaxLength || value[0] != '#' || !value[1..].All(Uri.IsHexDigit))
            return Validation.Invalid("Accent color must be a hex color like #0f766e.");
        return Validation.Ok;
    }
}
