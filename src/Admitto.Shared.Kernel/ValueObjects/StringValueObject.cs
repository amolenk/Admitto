using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public interface IStringValueObject
{
    string Value { get; }
}

public static class StringValueObject
{
    public static ValidationResult<TValueObject> TryFrom<TValueObject>(
        string? value,
        int maxLength,
        Func<string, TValueObject> factory)
        where TValueObject : IStringValueObject
    {
        if (string.IsNullOrWhiteSpace(value))
            return Errors.Empty;

        var normalized = value.Trim();

        if (normalized.Length > maxLength) return Errors.TooLong(maxLength);

        return ValidationResult<TValueObject>.Success(factory(normalized));
    }
    
    private static class Errors
    {
        public static readonly Error Empty = new(
            "text.empty",
            "Text is required.");

        public static Error TooLong(int maxLength) => new(
            "text.too_long",
            $"Text must be at most {maxLength} character(s).");
    }
}
