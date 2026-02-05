using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Registrations.Domain.Validation;

public static class RequiredText
{
    public static ValidationResult<string> TryNormalizeRequired(
        string? value,
        int minLength,
        int maxLength,
        Errors errors)
    {
        if (value is null) return errors.Empty;

        var v = value.Trim();

        if (string.IsNullOrWhiteSpace(v)) return errors.Empty;
        if (v.Length < minLength) return errors.TooShort;
        if (v.Length > maxLength) return errors.TooLong;

        return v;
    }

    public readonly record struct Errors(Error Empty, Error TooShort, Error TooLong);
}