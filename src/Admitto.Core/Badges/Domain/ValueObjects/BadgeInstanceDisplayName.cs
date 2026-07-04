using Vogen;

namespace Amolenk.Admitto.Core.Badges.Domain.ValueObjects;

[ValueObject<string>]
public partial struct BadgeInstanceDisplayName
{
    public const int MaxLength = 200;

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Badge instance display name cannot be empty.");
        if (value.Trim().Length > MaxLength)
            return Validation.Invalid($"Badge instance display name cannot exceed {MaxLength} characters.");
        return Validation.Ok;
    }

    private static string NormalizeInput(string value) => value.Trim();
}
