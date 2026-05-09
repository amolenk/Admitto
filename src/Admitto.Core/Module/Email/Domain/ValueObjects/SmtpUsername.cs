using Vogen;

namespace Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;

/// <summary>
/// SMTP authentication username. The aggregate enforces presence when AuthMode = Basic.
/// </summary>
[ValueObject<string>]
public partial struct SmtpUsername
{
    public const int MaxLength = 255;

    private static string NormalizeInput(string value) => value?.Trim() ?? string.Empty;

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("SMTP username is required.");
        if (value.Length > MaxLength)
            return Validation.Invalid($"SMTP username must be at most {MaxLength} character(s).");
        return Validation.Ok;
    }
}

