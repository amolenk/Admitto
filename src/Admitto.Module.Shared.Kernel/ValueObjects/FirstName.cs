using Vogen;

namespace Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

[ValueObject<string>]
public partial struct FirstName
{
    public const int MaxLength = 100;

    private static string NormalizeInput(string value) => value.Trim();

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("First name is required.");
        if (value.Length > MaxLength)
            return Validation.Invalid($"First name must be at most {MaxLength} character(s).");
        return Validation.Ok;
    }
}

