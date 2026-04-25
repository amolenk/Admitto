using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

/// <summary>
/// IANA time-zone identifier (e.g. <c>Europe/Amsterdam</c>, <c>UTC</c>).
/// Validated against <see cref="TimeZoneInfo.FindSystemTimeZoneById(string)"/>;
/// .NET 10 supports IANA ids on every platform.
/// </summary>
public readonly record struct TimeZoneId : IStringValueObject
{
    public const int MaxLength = 64;

    public string Value { get; }

    private TimeZoneId(string value) => Value = value;

    public static ValidationResult<TimeZoneId> TryFrom(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Errors.Empty;

        var normalized = input.Trim();

        if (normalized.Length > MaxLength)
            return Errors.TooLong;

        try
        {
            // .NET 10 normalises IANA ids on Windows (e.g. accepts "Europe/Amsterdam"
            // and returns the canonical id). We use the canonical id from the result
            // so persisted values are stable across platforms.
            var tz = TimeZoneInfo.FindSystemTimeZoneById(normalized);
            return ValidationResult<TimeZoneId>.Success(new TimeZoneId(tz.Id));
        }
        catch (TimeZoneNotFoundException)
        {
            return Errors.Unknown;
        }
        catch (InvalidTimeZoneException)
        {
            return Errors.Unknown;
        }
    }

    public static TimeZoneId From(string input) => TryFrom(input).GetValueOrThrow();

    public override string ToString() => Value;

    private static class Errors
    {
        public static readonly Error Empty = new(
            "time_zone.empty",
            "Time zone is required.");

        public static readonly Error TooLong = new(
            "time_zone.too_long",
            $"Time zone id must be at most {MaxLength} characters.");

        public static readonly Error Unknown = new(
            "time_zone.unknown",
            "Time zone id is not a recognised IANA zone (e.g. 'Europe/Amsterdam').");
    }
}
