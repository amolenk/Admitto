namespace Amolenk.Admitto.Domain.ValueObjects;

public record ReconfirmPolicy(
    TimeSpan WindowStartBeforeEvent,
    TimeSpan WindowEndBeforeEvent,
    TimeSpan InitialDelayAfterRegistration,
    TimeSpan ReminderInterval)
{
    /// <summary>
    /// Compute when the next reconfirmation email should be sent (if any).
    /// Returns null if outside the window or no send is due yet.
    /// </summary>
    public DateTimeOffset? NextSendAt(
        DateTimeOffset now,
        DateTimeOffset eventStartsAt,
        DateTimeOffset registeredAt,
        DateTimeOffset? lastSentAt)
    {
        // If not in window, do nothing.
        if (!IsInWindow(now, eventStartsAt)) return null;

        // First eligible send time: registration + initial delay, but not before the window opens.
        var windowOpensAt = eventStartsAt - WindowStartBeforeEvent;
        var firstEligible = Max(registeredAt + InitialDelayAfterRegistration, windowOpensAt);

        if (lastSentAt is null) return now >= firstEligible ? now : firstEligible;

        // Subsequent sends are spaced by the interval.
        if (ReminderInterval <= TimeSpan.Zero) return null; // interval disabled
        
        var next = lastSentAt.Value + ReminderInterval;
        return now >= next ? now : next;
    }

    private bool IsInWindow(DateTimeOffset now, DateTimeOffset eventStartsAt)
        => now <= eventStartsAt - WindowEndBeforeEvent &&
           now >= eventStartsAt - WindowStartBeforeEvent;

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b)
        => a >= b ? a : b;
}