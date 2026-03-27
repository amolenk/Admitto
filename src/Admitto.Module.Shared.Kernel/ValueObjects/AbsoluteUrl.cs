using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

public readonly record struct AbsoluteUrl : IUriValueObject
{
    // The practical maximum length for a URI seems to be around 2000 characters,
    // but most URIs are much shorter. Setting a limit of 320 characters
    // to cover most use cases while preventing excessively long URIs.
    // This aligns with the maximum length for email addresses defined in RFC 5321.
    private const int MaxLength = 320;

    public Uri Value { get; }

    private AbsoluteUrl(Uri normalizedValue)
    {
        Value = normalizedValue;
    }
    
    public static ValidationResult<AbsoluteUrl> TryFrom(string? input)
        => NormalizeAndValidate(input)
            .Map(normalized => new AbsoluteUrl(normalized));

    public static AbsoluteUrl From(string input)
        => TryFrom(input).GetValueOrThrow();

    private static ValidationResult<Uri> NormalizeAndValidate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Errors.Empty;

        var normalized = value.Trim();

        if (normalized.Length > MaxLength) return Errors.TooLong;

        return !Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            ? Errors.InvalidFormat
            : ValidationResult<Uri>.Success(uri);
    }
    
    private static class Errors
    {
        public static readonly Error Empty = new(
            "url.empty",
            "URL is required.");

        public static readonly Error TooLong = new(
            "url.too_long",
            $"URL must be at most {MaxLength} character(s).");

        public static readonly Error InvalidFormat = new(
            "url.invalid_format",
            $"URL has an invalid format.");
    }
}



