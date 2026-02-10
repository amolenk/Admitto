using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Registrations.Domain.ValueObjects;

public readonly record struct FirstName : IStringValueObject
{
    private const int MinLength = 1;
    private const int MaxLength = 100;

    public string Value { get; }

    private FirstName(string value) => Value = value;

    public static ValidationResult<FirstName> TryFrom(string? value)
        => StringValueObject.TryFrom(
            value,
            MinLength,
            MaxLength,
            v => new FirstName(v),
            Errors.Empty,
            Errors.TooShort,
            Errors.TooLong);

    public static FirstName From(string? value)
        => StringValueObject.From(
            value,
            MinLength,
            MaxLength,
            v => new FirstName(v),
            Errors.Empty,
            Errors.TooShort,
            Errors.TooLong);

    public override string ToString() => Value;

    private static class Errors
    {
        public static readonly Error Empty = new(
            "first_name.empty",
            "First name is required.");

        public static readonly Error TooShort = new(
            "first_name.too_short",
            $"First name must be at least {MinLength} character(s).");

        public static readonly Error TooLong = new(
            "first_name.too_long",
            $"First name must be at most {MaxLength} character(s).");
    }
}