using Vogen;

namespace Amolenk.Admitto.Module.Email.Domain.ValueObjects;

/// <summary>
/// SMTP server hostname. Permissive: non-empty, trimmed, length-capped — no DNS or RFC parsing.
/// </summary>
[ValueObject<string>]
public partial struct Hostname
{
    public const int MaxLength = 255;

    private static string NormalizeInput(string value) => value?.Trim() ?? string.Empty;

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Hostname is required.");
        if (value.Length > MaxLength)
            return Validation.Invalid($"Hostname must be at most {MaxLength} character(s).");
        return Validation.Ok;
    }
}

