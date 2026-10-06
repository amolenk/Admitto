namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

/// <summary>
/// Computes the <c>ExpiresAt</c> timestamp for a waitlist coupon, applying quiet-hours logic so
/// that the attendee always gets the full <paramref name="claimWindowHours"/> during waking hours.
/// </summary>
/// <remarks>
/// The claim clock pauses during every event-local quiet-hours period, preserving any remaining waking time
/// when quiet hours end. An offer issued during quiet hours starts its clock at their end. The result is clamped to
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

        var remaining = TimeSpan.FromHours(claimWindowHours);
        var expiresAt = PushPastQuietHours(utcNow, tz, quietHoursStart, quietHoursEnd);

        if (quietHoursStart == quietHoursEnd)
        {
            expiresAt = expiresAt.Add(remaining);
        }
        else
        {
            while (remaining > TimeSpan.Zero)
            {
                var local = TimeZoneInfo.ConvertTimeFromUtc(expiresAt.UtcDateTime, tz);
                var nextQuietStartLocal = local.Date.Add(quietHoursStart.ToTimeSpan());
                if (nextQuietStartLocal < local)
                    nextQuietStartLocal = nextQuietStartLocal.AddDays(1);

                var nextQuietStart = ToUtc(nextQuietStartLocal, tz);
                var wakingTime = nextQuietStart - expiresAt;
                if (remaining <= wakingTime)
                {
                    expiresAt = expiresAt.Add(remaining);
                    break;
                }

                remaining -= wakingTime;
                expiresAt = PushPastQuietHours(nextQuietStart, tz, quietHoursStart, quietHoursEnd);
            }
        }

        // If the final waking hour ends exactly when quiet hours begin, keep the deadline out of quiet hours.
        expiresAt = PushPastQuietHours(expiresAt, tz, quietHoursStart, quietHoursEnd);

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

        return ToUtc(nextAllowedDate.Add(quietHoursEnd.ToTimeSpan()), tz);
    }

    private static DateTimeOffset ToUtc(DateTime local, TimeZoneInfo tz)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);

        // The computed local time may not exist (a spring-forward DST gap) — advance to the
        // first valid local time after the gap instead of letting ConvertTimeToUtc throw.
        while (tz.IsInvalidTime(local))
            local = local.AddMinutes(1);

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, tz));
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
