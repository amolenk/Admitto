namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

/// <summary>
/// Computes the <c>ExpiresAt</c> timestamp for a waitlist coupon, applying quiet-hours logic so
/// that the attendee always gets the full <paramref name="claimWindowHours"/> during waking hours.
/// </summary>
/// <remarks>
/// Formula: <c>ExpiresAt = min(pushPastQuietHours(pushPastQuietHours(utcNow) + claimWindowHours), eventStartsAt)</c>.
/// <c>pushPastQuietHours</c> advances an instant that falls inside quiet hours to the moment quiet hours end
/// (converted to UTC), leaving it unchanged otherwise. It is applied both when the notification is sent (so an
/// offer issued during quiet hours doesn't start its window then) and to the computed expiry (so an offer issued
/// just before quiet hours doesn't expire during them either) — guaranteeing the attendee always gets the full
/// <paramref name="claimWindowHours"/> during waking hours. The result is clamped to
/// <paramref name="eventStartsAt"/> so an offer never outlives the event it's for, unless the event has already
/// started — there is no valid future expiry to clamp to in that case, so the uncapped value is returned instead.
/// A quiet-hours end that falls in a DST spring-forward gap is advanced to the first valid local time after the
/// gap rather than throwing.
/// </remarks>
public static class WaitlistClaimWindowCalculator
{
    public static DateTimeOffset ComputeExpiresAt(
        DateTimeOffset utcNow,
        TimeZoneId timeZoneId,
        TimeOnly quietHoursStart,
        TimeOnly quietHoursEnd,
        int claimWindowHours,
        DateTimeOffset eventStartsAt)
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

        var windowStart = PushPastQuietHours(utcNow, tz, quietHoursStart, quietHoursEnd);
        var expiresAt = PushPastQuietHours(
            windowStart.Add(TimeSpan.FromHours(claimWindowHours)), tz, quietHoursStart, quietHoursEnd);

        // Only clamp down to the event's start when that's still a real, future upper bound — an event that
        // already started is a degenerate case this calculator can't fix by handing back an already-lapsed
        // expiry, so it falls back to the uncapped value rather than making Coupon.Create reject the offer.
        if (eventStartsAt > utcNow && expiresAt > eventStartsAt)
            return eventStartsAt;

        return expiresAt;
    }

    /// <summary>
    /// Returns <paramref name="instant"/> unchanged unless it falls inside quiet hours in the given time zone, in
    /// which case it advances to the moment quiet hours end. DST-safe: a quiet-hours end that falls in a
    /// spring-forward gap advances to the first valid local time after the gap instead of throwing.
    /// </summary>
    private static DateTimeOffset PushPastQuietHours(
        DateTimeOffset instant, TimeZoneInfo tz, TimeOnly quietHoursStart, TimeOnly quietHoursEnd)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(instant.UtcDateTime, tz);
        var localTime = TimeOnly.FromDateTime(local);

        if (!IsInQuietHours(localTime, quietHoursStart, quietHoursEnd))
            return instant;

        // Determine the calendar date of the next quietHoursEnd moment.
        var nextAllowedDate = local.Date;
        // Quiet hours that span midnight: if the current time is on or after the start
        // (i.e., after midnight-crossing start, e.g. 23:00), quietHoursEnd falls on the
        // next calendar day.
        if (quietHoursStart > quietHoursEnd && localTime >= quietHoursStart)
            nextAllowedDate = nextAllowedDate.AddDays(1);

        var nextAllowedLocal = DateTime.SpecifyKind(
            nextAllowedDate.Add(quietHoursEnd.ToTimeSpan()), DateTimeKind.Unspecified);

        // The computed local time may not exist (a spring-forward DST gap) — advance to the
        // first valid local time after the gap instead of letting ConvertTimeToUtc throw.
        while (tz.IsInvalidTime(nextAllowedLocal))
            nextAllowedLocal = nextAllowedLocal.AddMinutes(1);

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(nextAllowedLocal, tz));
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
