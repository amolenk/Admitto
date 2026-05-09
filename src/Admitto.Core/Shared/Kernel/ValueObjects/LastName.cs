using Vogen;

namespace Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

[ValueObject<string>]
public partial struct LastName
{
    public const int MaxLength = 100;

    private static string NormalizeInput(string value) => value.Trim();

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Last name is required.");
        if (value.Length > MaxLength)
            return Validation.Invalid($"Last name must be at most {MaxLength} character(s).");
        return Validation.Ok;
    }
}

