using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Humanizer;

namespace Amolenk.Admitto.Shared.Kernel.ValueObjects;

public abstract record Slug : IStringValueObject
{
    public const int MaxLength = 100;
    
    public string Value { get; }

    public static implicit operator string(Slug slug) => slug.Value;

    protected Slug(string normalizedValue)
    {
        Value = normalizedValue;
    }

    protected static ValidationResult<string> NormalizeAndValidate(string? input)
    {
        if (input is null)
            return Errors.Required();

        var normalized = input.Kebaberize();

        if (string.IsNullOrWhiteSpace(input))
            return Errors.Empty();

        if (normalized.Length > MaxLength)
            return Errors.TooLong(MaxLength);

        return normalized;
    }
    
    private static class Errors
    {
        private const string Name = "slug";
    
        public static Error Required() => SharedErrors.ValueObjects.Required(Name);
        public static Error Empty() => SharedErrors.ValueObjects.Empty(Name);
        public static Error TooLong(int max) => SharedErrors.ValueObjects.TooLong(Name, max);
    }
}

