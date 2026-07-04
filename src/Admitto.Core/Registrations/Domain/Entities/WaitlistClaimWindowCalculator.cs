namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

/// <summary>
/// Computes the <c>ExpiresAt</c> timestamp for a waitlist coupon, applying quiet-hours logic so
/// that the attendee always gets the full <paramref name="claimWindowHours"/> during waking hours.
/// </summary>
/// <remarks>
/// Formula: <c>ExpiresAt = max(utcNow, nextAllowedWindowStart) + claimWindowHours</c>
/// where <c>nextAllowedWindowStart</c> is the moment quiet hours end (converted to UTC)
/// when the notification is sent inside the quiet window; otherwise it equals <c>utcNow</c>.
/// </remarks>
public static class WaitlistClaimWindowCalculator
{
    public static DateTimeOffset ComputeExpiresAt(
        DateTimeOffset utcNow,
        TimeZoneId timeZoneId,
        TimeOnly quietHoursStart,
        TimeOnly quietHoursEnd,
        int claimWindowHours)
    {
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Value);
        }
        catch (TimeZoneNotFoundException)
        {
            tz = TimeZoneInfo.Utc;
        }

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow.UtcDateTime, tz);
        var localNowTime = TimeOnly.FromDateTime(localNow);

        DateTimeOffset windowStart;
        if (IsInQuietHours(localNowTime, quietHoursStart, quietHoursEnd))
        {
            // Determine the calendar date of the next quietHoursEnd moment.
            var nextAllowedDate = localNow.Date;
            // Quiet hours that span midnight: if the current time is on or after the start
            // (i.e., after midnight-crossing start, e.g. 23:00), quietHoursEnd falls on the
            // next calendar day.
            if (quietHoursStart > quietHoursEnd && localNowTime >= quietHoursStart)
                nextAllowedDate = nextAllowedDate.AddDays(1);

            var nextAllowedLocal = nextAllowedDate.Add(quietHoursEnd.ToTimeSpan());
            windowStart = new DateTimeOffset(
                TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(nextAllowedLocal, DateTimeKind.Unspecified), tz));
        }
        else
        {
            windowStart = utcNow;
        }

        return windowStart.Add(TimeSpan.FromHours(claimWindowHours));
    }

    private static bool IsInQuietHours(TimeOnly time, TimeOnly start, TimeOnly end)
    {
        if (start == end)
            return false; // No quiet hours configured

        if (start > end) // spans midnight (e.g., 22:00–08:00)
            return time >= start || time < end;

        return time >= start && time < end; // same-day window (e.g., 13:00–15:00)
    }
}
