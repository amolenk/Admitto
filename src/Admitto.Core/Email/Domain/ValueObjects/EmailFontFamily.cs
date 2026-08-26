using Vogen;

namespace Amolenk.Admitto.Core.Email.Domain.ValueObjects;

/// <summary>
/// CSS font-family string used by built-in email templates.
/// <para>
/// This is deliberately a fixed, system-wide constant: font family is not team-owned
/// branding and is not persisted anywhere. Only <see cref="Default"/> is ever used at
/// runtime. Do not add a per-team font column without an ADR.
/// </para>
/// </summary>
[ValueObject<string>]
public partial struct EmailFontFamily
{
    public const string Default = "Inter, sans-serif";
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
