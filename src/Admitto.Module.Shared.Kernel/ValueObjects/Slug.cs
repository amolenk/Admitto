using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Humanizer;

namespace Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

public readonly record struct Slug : IStringValueObject
{
    public const int MaxLength = 64;

    public string Value { get; }

    private Slug(string normalizedValue)
    {
        Value = normalizedValue;
    }
    
    public static ValidationResult<Slug> TryFrom(string? input)
        => NormalizeAndValidate(input)
            .Map(normalized => new Slug(normalized));

    public static Slug From(string input)
        => TryFrom(input).GetValueOrThrow();

    private static ValidationResult<string> NormalizeAndValidate(string? input)
    {
        if (input is null)
            return Errors.Empty;

        var normalized = input.Trim().Kebaberize();

        if (string.IsNullOrWhiteSpace(input))
            return Errors.Empty;
        
        if (normalized.Length > MaxLength)
            return Errors.TooLong;

        return normalized;
    }
    
    private static class Errors
    {
        public static readonly Error Empty = new(
            "slug.empty",
            "Slug is required.");

        public static readonly Error TooLong = new(
            "slug.too_long",
            $"Slug must be at most {MaxLength} character(s).");
    }
}



