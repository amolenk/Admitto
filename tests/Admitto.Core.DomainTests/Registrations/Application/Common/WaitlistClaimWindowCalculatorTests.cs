using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Core.Registrations.Application.Tests.Common;

[TestClass]
public sealed class WaitlistClaimWindowCalculatorTests
{
    private static readonly TimeZoneId Amsterdam = TimeZoneId.From("Europe/Amsterdam");
    private static readonly TimeOnly QuietStart = new(22, 0);  // 22:00
    private static readonly TimeOnly QuietEnd   = new(8, 0);   // 08:00
    private static readonly DateTimeOffset FarFutureEventStart = new(2099, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset UtcAt(int localHour, TimeZoneInfo tz)
    {
        var localDate = new DateTime(2026, 6, 15, localHour, 0, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localDate, tz));
    }

    // Given the current time is outside quiet hours
    // When the claim expiry is computed
    // Then it returns the current time plus the claim window
    [TestMethod]
    public void ComputeExpiresAt_OutsideQuietHours_ReturnsUtcNowPlusClaimWindow()
    {
        // Arrange
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(10, tz); // 10:00 local — outside quiet window

        // Act
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(utcNow, Amsterdam, QuietStart, QuietEnd, 8, FarFutureEventStart);

        // Assert
        result.ShouldBe(utcNow.AddHours(8), tolerance: TimeSpan.FromSeconds(1));
    }

    // Given the current time is inside quiet hours, before midnight
    // When the claim expiry is computed
    // Then it expires at next day's quiet-hours end plus the claim window
    [TestMethod]
    public void ComputeExpiresAt_InsideQuietHours_BeforeMidnight_ExpiresAtQuietEndPlusClaimWindow()
    {
        // Arrange — 23:00 local, inside the 22:00–08:00 window (past start, before midnight)
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(23, tz);

        // Act
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(utcNow, Amsterdam, QuietStart, QuietEnd, 8, FarFutureEventStart);

        // Assert — window opens at 08:00 next day local (2026-06-16 08:00) + 8 hours
        var expectedWindowStart = UtcAt(8, tz).AddDays(1); // 08:00 next day UTC-equivalent
        result.ShouldBe(expectedWindowStart.AddHours(8), tolerance: TimeSpan.FromSeconds(1));
    }

    // Given the current time is inside quiet hours, in the early morning after midnight
    // When the claim expiry is computed
    // Then it expires at the same day's quiet-hours end plus the claim window
    [TestMethod]
    public void ComputeExpiresAt_InsideQuietHours_EarlyMorning_ExpiresAtSameDayQuietEndPlusClaimWindow()
    {
        // Arrange — 03:00 local, inside the 22:00–08:00 window (after midnight, before end)
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(3, tz);

        // Act
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(utcNow, Amsterdam, QuietStart, QuietEnd, 8, FarFutureEventStart);

        // Assert — window opens at 08:00 same day local + 8 hours
        var expectedWindowStart = UtcAt(8, tz); // 08:00 same day
        result.ShouldBe(expectedWindowStart.AddHours(8), tolerance: TimeSpan.FromSeconds(1));
    }

    // Given the current time is outside quiet hours
    // When the claim expiry is computed with a 24-hour claim window
    // Then it preserves all 24 waking hours across the overnight quiet period
    [TestMethod]
    public void ComputeExpiresAt_CustomClaimWindow_UsesProvidedHours()
    {
        // Arrange
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(12, tz);

        // Act — 24-hour claim window
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(utcNow, Amsterdam, QuietStart, QuietEnd, 24, FarFutureEventStart);

        // Assert
        // The last waking hour ends at 22:00 tomorrow, so the deadline moves to the next quiet-hours end.
        result.ShouldBe(UtcAt(8, tz).AddDays(2));
    }

    // Given a same-day quiet window and a current time inside it
    // When the claim expiry is computed
    // Then it expires at the quiet-hours end that same day plus the claim window
    [TestMethod]
    public void ComputeExpiresAt_SameDayQuietHours_TreatedCorrectly()
    {
        // Arrange — same-day quiet window: 13:00–15:00
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(14, tz); // inside 13:00–15:00 local
        var quietStart = new TimeOnly(13, 0);
        var quietEnd = new TimeOnly(15, 0);

        // Act
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(utcNow, Amsterdam, quietStart, quietEnd, 8, FarFutureEventStart);

        // Assert — window opens at 15:00 same day + 8 hours
        var expectedWindowStart = UtcAt(15, tz);
        result.ShouldBe(expectedWindowStart.AddHours(8), tolerance: TimeSpan.FromSeconds(1));
    }

    // Given the quiet-hours start and end are equal, meaning no quiet hours are configured
    // When the claim expiry is computed
    // Then it returns the current time plus the claim window
    [TestMethod]
    public void ComputeExpiresAt_NoQuietHours_StartEqualsEnd_ReturnsUtcNowPlusClaimWindow()
    {
        // Arrange — same start/end = no quiet hours
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(23, tz);
        var noQuiet = new TimeOnly(0, 0);

        // Act
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(utcNow, Amsterdam, noQuiet, noQuiet, 8, FarFutureEventStart);

        // Assert
        result.ShouldBe(utcNow.AddHours(8), tolerance: TimeSpan.FromSeconds(1));
    }

    // Given an evening offer whose claim window crosses overnight quiet hours
    // When the claim expiry is computed
    // Then the remaining two waking hours resume after quiet hours end
    [TestMethod]
    public void ComputeExpiresAt_WindowWouldEndDuringQuietHours_PreservesRemainingWakingHours()
    {
        // Arrange — issued 20:00 local (outside 22:00-08:00 quiet hours); a naive 4-hour window would
        // expire at 00:00 local, inside quiet hours
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(20, tz);

        // Act
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(
            utcNow, Amsterdam, QuietStart, QuietEnd, 4, FarFutureEventStart);

        // Assert — two hours before quiet hours and two after, expiring at 10:00 the next day
        var expectedExpiresAt = UtcAt(10, tz).AddDays(1);
        result.ShouldBe(expectedExpiresAt, tolerance: TimeSpan.FromSeconds(1));
    }

    // Given quiet hours from 20:00 to 08:00 and an eight-hour offer issued at 18:00
    // When the claim expiry is computed
    // Then two hours count before quiet hours and six after, expiring at 14:00 the next day
    [TestMethod]
    public void ComputeExpiresAt_OfferAt18WithQuietHours20To08_ExpiresAt14NextDay()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(18, tz);

        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(
            utcNow, Amsterdam, new TimeOnly(20, 0), QuietEnd, 8, FarFutureEventStart);

        result.ShouldBe(UtcAt(14, tz).AddDays(1));
    }

    // Given an offer issued exactly when quiet hours start or end
    // When an eight-hour claim expiry is computed
    // Then the claim clock starts at 08:00 and expires at 16:00
    [TestMethod]
    [DataRow(20, 1)]
    [DataRow(8, 0)]
    public void ComputeExpiresAt_AtQuietHoursBoundary_CountsFullWindowAfterQuietEnd(int localHour, int daysLater)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);

        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(
            UtcAt(localHour, tz), Amsterdam, new TimeOnly(20, 0), QuietEnd, 8, FarFutureEventStart);

        result.ShouldBe(UtcAt(16, tz).AddDays(daysLater));
    }

    // Given an offer with exactly two waking hours left before quiet hours begin
    // When a two-hour claim expiry is computed
    // Then the deadline moves to 08:00 the next day rather than expiring at the quiet-hours start
    [TestMethod]
    public void ComputeExpiresAt_WindowEndsAtQuietStart_ExpiresAtQuietEnd()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);

        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(
            UtcAt(18, tz), Amsterdam, new TimeOnly(20, 0), QuietEnd, 2, FarFutureEventStart);

        result.ShouldBe(UtcAt(8, tz).AddDays(1));
    }

    // Given a 32-hour offer issued at 18:00 with daily quiet hours from 20:00 to 08:00
    // When the claim expiry spans three quiet periods
    // Then all 32 waking hours are preserved, expiring at 14:00 three days later
    [TestMethod]
    public void ComputeExpiresAt_WindowCrossesMultipleQuietPeriods_PreservesAllWakingHours()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(18, tz);

        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(
            utcNow, Amsterdam, new TimeOnly(20, 0), QuietEnd, 32, FarFutureEventStart);

        result.ShouldBe(UtcAt(14, tz).AddDays(3));
    }

    // Given an eight-hour offer issued before a same-day quiet period from 13:00 to 15:00
    // When the claim expiry is computed
    // Then the clock pauses for two hours even though the naive expiry is outside quiet hours
    [TestMethod]
    public void ComputeExpiresAt_WindowCrossesSameDayQuietHours_PreservesAllWakingHours()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(10, tz);

        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(
            utcNow, Amsterdam, new TimeOnly(13, 0), new TimeOnly(15, 0), 8, FarFutureEventStart);

        result.ShouldBe(UtcAt(20, tz));
    }

    // Given an evening offer before the spring-forward night with quiet hours from 20:00 to 08:00
    // When an eight-hour claim expiry is computed across the DST change
    // Then two waking hours count before the night and six after, expiring at 14:00 local
    [TestMethod]
    public void ComputeExpiresAt_WindowCrossesSpringForward_PreservesWakingHoursInEventTimeZone()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var localNow = new DateTime(2026, 3, 28, 18, 0, 0, DateTimeKind.Unspecified);
        var utcNow = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localNow, tz));

        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(
            utcNow, Amsterdam, new TimeOnly(20, 0), QuietEnd, 8, FarFutureEventStart);

        result.ShouldBe(new DateTimeOffset(2026, 3, 29, 12, 0, 0, TimeSpan.Zero));
    }

    // Given a quiet-hours end that falls inside a DST spring-forward gap (a local time that never occurs)
    // When the claim expiry is computed
    // Then it does not throw, advancing to the first valid local time after the gap
    [TestMethod]
    public void ComputeExpiresAt_QuietHoursEndInDstGap_DoesNotThrowAndAdvancesPastGap()
    {
        // Arrange — Europe/Amsterdam springs forward from 02:00 to 03:00 on 2026-03-29, so 02:30 never occurs
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var localBeforeGap = new DateTime(2026, 3, 29, 0, 30, 0, DateTimeKind.Unspecified);
        var utcNow = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localBeforeGap, tz));
        var quietStart = new TimeOnly(22, 0);
        var quietEnd = new TimeOnly(2, 30); // inside the gap

        // Act
        var result = Should.NotThrow(() =>
            WaitlistClaimWindowCalculator.ComputeExpiresAt(
                utcNow, Amsterdam, quietStart, quietEnd, 8, FarFutureEventStart));

        // Assert — window opens at the first valid local time after the gap (03:00 CEST = 01:00 UTC) + 8 hours
        var expectedWindowStart = new DateTimeOffset(2026, 3, 29, 1, 0, 0, TimeSpan.Zero);
        result.ShouldBe(expectedWindowStart.AddHours(8), tolerance: TimeSpan.FromSeconds(1));
    }

    // Given a claim window that would otherwise extend past the event's start time
    // When the claim expiry is computed
    // Then it is clamped to the event's start time
    [TestMethod]
    public void ComputeExpiresAt_WindowWouldOutlastEvent_ClampsToEventStart()
    {
        // Arrange
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(10, tz); // outside quiet hours
        var eventStartsAt = utcNow.AddHours(5); // the event starts before the 48-hour window would end

        // Act
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(
            utcNow, Amsterdam, QuietStart, QuietEnd, 48, eventStartsAt);

        // Assert
        result.ShouldBe(eventStartsAt);
    }

    // Given an event that has already started
    // When the claim expiry is computed
    // Then it falls back to the uncapped expiry instead of clamping to an already-past event start
    [TestMethod]
    public void ComputeExpiresAt_EventAlreadyStarted_FallsBackToUncappedExpiry()
    {
        // Arrange
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(10, tz); // outside quiet hours
        var eventStartsAt = utcNow.AddHours(-1); // the event already started

        // Act
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(
            utcNow, Amsterdam, QuietStart, QuietEnd, 8, eventStartsAt);

        // Assert
        result.ShouldBe(utcNow.AddHours(8), tolerance: TimeSpan.FromSeconds(1));
    }
}
