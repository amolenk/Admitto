using Vogen;

namespace Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

/// <summary>
/// IANA time-zone identifier (e.g. <c>Europe/Amsterdam</c>, <c>UTC</c>).
/// Validated against <see cref="TimeZoneInfo.FindSystemTimeZoneById(string)"/>;
/// .NET 10 supports IANA ids on every platform.
/// </summary>
[ValueObject<string>]
public partial struct TimeZoneId
{
    public const int MaxLength = 64;

    private static string NormalizeInput(string value)
    {
        var trimmed = value.Trim();
        try { return TimeZoneInfo.FindSystemTimeZoneById(trimmed).Id; }
        catch { return trimmed; }
    }

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Time zone is required.");
        if (value.Length > MaxLength)
            return Validation.Invalid($"Time zone id must be at most {MaxLength} characters.");
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(value);
            return Validation.Ok;
        }
        catch
        {
            return Validation.Invalid("Time zone id is not a recognised IANA zone (e.g. 'Europe/Amsterdam').");
        }
    }
}

