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

    private static DateTimeOffset UtcAt(int localHour, TimeZoneInfo tz)
    {
        var localDate = new DateTime(2026, 6, 15, localHour, 0, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localDate, tz));
    }

    [TestMethod]
    public void ComputeExpiresAt_OutsideQuietHours_ReturnsUtcNowPlusClaimWindow()
    {
        // Arrange
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(10, tz); // 10:00 local — outside quiet window

        // Act
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(utcNow, Amsterdam, QuietStart, QuietEnd, 8);

        // Assert
        result.ShouldBe(utcNow.AddHours(8), tolerance: TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void ComputeExpiresAt_InsideQuietHours_BeforeMidnight_ExpiresAtQuietEndPlusClaimWindow()
    {
        // Arrange — 23:00 local, inside the 22:00–08:00 window (past start, before midnight)
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(23, tz);

        // Act
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(utcNow, Amsterdam, QuietStart, QuietEnd, 8);

        // Assert — window opens at 08:00 next day local (2026-06-16 08:00) + 8 hours
        var expectedWindowStart = UtcAt(8, tz).AddDays(1); // 08:00 next day UTC-equivalent
        result.ShouldBe(expectedWindowStart.AddHours(8), tolerance: TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void ComputeExpiresAt_InsideQuietHours_EarlyMorning_ExpiresAtSameDayQuietEndPlusClaimWindow()
    {
        // Arrange — 03:00 local, inside the 22:00–08:00 window (after midnight, before end)
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(3, tz);

        // Act
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(utcNow, Amsterdam, QuietStart, QuietEnd, 8);

        // Assert — window opens at 08:00 same day local + 8 hours
        var expectedWindowStart = UtcAt(8, tz); // 08:00 same day
        result.ShouldBe(expectedWindowStart.AddHours(8), tolerance: TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void ComputeExpiresAt_CustomClaimWindow_UsesProvidedHours()
    {
        // Arrange
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(12, tz);

        // Act — 24-hour claim window
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(utcNow, Amsterdam, QuietStart, QuietEnd, 24);

        // Assert
        result.ShouldBe(utcNow.AddHours(24), tolerance: TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void ComputeExpiresAt_SameDayQuietHours_TreatedCorrectly()
    {
        // Arrange — same-day quiet window: 13:00–15:00
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(14, tz); // inside 13:00–15:00 local
        var quietStart = new TimeOnly(13, 0);
        var quietEnd = new TimeOnly(15, 0);

        // Act
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(utcNow, Amsterdam, quietStart, quietEnd, 8);

        // Assert — window opens at 15:00 same day + 8 hours
        var expectedWindowStart = UtcAt(15, tz);
        result.ShouldBe(expectedWindowStart.AddHours(8), tolerance: TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void ComputeExpiresAt_NoQuietHours_StartEqualsEnd_ReturnsUtcNowPlusClaimWindow()
    {
        // Arrange — same start/end = no quiet hours
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Amsterdam.Value);
        var utcNow = UtcAt(23, tz);
        var noQuiet = new TimeOnly(0, 0);

        // Act
        var result = WaitlistClaimWindowCalculator.ComputeExpiresAt(utcNow, Amsterdam, noQuiet, noQuiet, 8);

        // Assert
        result.ShouldBe(utcNow.AddHours(8), tolerance: TimeSpan.FromSeconds(1));
    }
}
