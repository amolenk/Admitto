using Vogen;

namespace Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

[ValueObject<string>]
public partial struct DisplayName
{
    public const int MaxLength = 64;

    private static string NormalizeInput(string value) => value.Trim();

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Display name is required.");
        if (value.Length > MaxLength)
            return Validation.Invalid($"Display name must be at most {MaxLength} character(s).");
        return Validation.Ok;
    }
}
