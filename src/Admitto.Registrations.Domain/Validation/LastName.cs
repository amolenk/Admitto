using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Registrations.Domain.Validation;

public static class LastName
{
    public const int MinLength = 1;
    public const int MaxLength = 100;

    private static readonly RequiredText.Errors RequiredTextErrors =
        new(Errors.Empty, Errors.TooShort, Errors.TooLong);

    public static ValidationResult<string> TryNormalize(string? value) =>
        RequiredText.TryNormalizeRequired(value, MinLength, MaxLength, RequiredTextErrors);

    public static string Normalize(string? value) =>
        TryNormalize(value).GetValueOrThrow();

    private static class Errors
    {
        public static readonly Error Empty = new(
            "reg.last_name.empty",
            "Last name is required.");

        public static readonly Error TooShort = new(
            "reg.last_name.too_short",
            $"Last name must be at least {MinLength} character(s).");

        public static readonly Error TooLong = new(
            "reg.last_name.too_long",
            $"Last name must be at most {MaxLength} character(s).");
    }
}