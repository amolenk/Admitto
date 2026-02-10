using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.ValueObjects;

public readonly record struct WebsiteUrl : IUriValueObject
{
    private const int MaxLength = 100;

    public Uri Value { get; }

    private WebsiteUrl(Uri value) => Value = value;

    public static ValidationResult<WebsiteUrl> TryFrom(string? value)
        => UriValueObject.TryFrom(
            value,
            v => new WebsiteUrl(v),
            Errors.Empty,
            Errors.TooLong,
            Errors.InvalidFormat);

    public static WebsiteUrl From(string? value) => TryFrom(value).GetValueOrThrow();

    public override string ToString() => Value.ToString();

    private static class Errors
    {
        public static readonly Error Empty = new(
            "website_url.empty",
            "Website URL is required.");

        public static readonly Error TooLong = new(
            "website_url.too_long",
            $"Website URL must be at most {MaxLength} character(s).");

        public static readonly Error InvalidFormat = new(
            "website_url.invalid_format",
            $"Website URL has an invalid format.");
    }
}