using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Registrations.Domain.Validation;

public static class FirstName
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
            "reg.first_name.empty",
            "First name is required.");

        public static readonly Error TooShort = new(
            "reg.first_name.too_short",
            $"First name must be at least {MinLength} character(s).");

        public static readonly Error TooLong = new(
            "reg.first_name.too_long",
            $"First name must be at most {MaxLength} character(s).");
    }
}

// public readonly record struct FirstNameVO
// {
//     public const int MinLength = 1;
//     public const int MaxLength = 100;
//
//     private static readonly RequiredText.Errors RequiredTextErrors =
//         new(Errors.Empty, Errors.TooShort, Errors.TooLong);
//
//     public string Value { get; }
//
//     private FirstNameVO(string value) => Value = value;
//
//     public static Result<FirstNameVO> TryFrom(string? value) =>
//         RequiredText.TryNormalizeRequired(value, MinLength, MaxLength, RequiredTextErrors)
//             .Map(v => new FirstNameVO(v));
//
//     public static FirstNameVO From(string? value) =>
//         TryFrom(value).GetValueOrThrow();
//
//     public override string ToString() => Value;
//
//     private static class Errors
//     {
//         public static readonly Error Empty = new("reg.first_name.empty", "First name is required.");
//
//         public static readonly Error TooShort = new(
//             "reg.first_name.too_short",
//             $"First name must be at least {MinLength} character(s).");
//
//         public static readonly Error TooLong = new(
//             "reg.first_name.too_long",
//             $"First name must be at most {MaxLength} character(s).");
//     }
// }