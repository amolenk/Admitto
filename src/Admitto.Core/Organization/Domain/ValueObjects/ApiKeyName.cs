using Vogen;

namespace Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

[ValueObject<string>]
public partial struct ApiKeyName
{
    public const int MaxLength = 100;

    private static string NormalizeInput(string value) => value.Trim();

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("API key name is required.");
        if (value.Length > MaxLength)
            return Validation.Invalid($"API key name must be at most {MaxLength} character(s).");
        return Validation.Ok;
    }
}
