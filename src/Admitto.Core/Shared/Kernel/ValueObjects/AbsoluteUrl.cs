using Vogen;

namespace Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

[ValueObject<string>]
public partial struct AbsoluteUrl
{
    private const int MaxLength = 320;

    private static string NormalizeInput(string value) => value.Trim();

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("URL is required.");
        if (value.Length > MaxLength)
            return Validation.Invalid($"URL must be at most {MaxLength} character(s).");
        if (!Uri.TryCreate(value, UriKind.Absolute, out _))
            return Validation.Invalid("URL has an invalid format.");
        return Validation.Ok;
    }
}

