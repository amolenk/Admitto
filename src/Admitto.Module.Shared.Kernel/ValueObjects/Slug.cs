using Humanizer;
using Vogen;

namespace Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

[ValueObject<string>]
public partial struct Slug
{
    public const int MaxLength = 64;

    private static string NormalizeInput(string value) => value.Trim().Kebaberize();

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Slug is required.");
        if (value.Length > MaxLength)
            return Validation.Invalid($"Slug must be at most {MaxLength} character(s).");
        return Validation.Ok;
    }
}

