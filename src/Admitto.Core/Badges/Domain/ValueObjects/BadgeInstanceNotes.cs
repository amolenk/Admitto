using Vogen;

namespace Amolenk.Admitto.Core.Badges.Domain.ValueObjects;

[ValueObject<string>]
public partial struct BadgeInstanceNotes
{
    public const int MaxLength = 500;

    private static Validation Validate(string value)
    {
        if (value.Length > MaxLength)
            return Validation.Invalid($"Badge instance notes cannot exceed {MaxLength} characters.");
        return Validation.Ok;
    }
}
