using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

/// <summary>
/// Value-object policy describing the reconfirmation window, per-attendee
/// minimum email interval, and optional event-local quiet hours.
/// </summary>
public sealed record TicketedEventReconfirmPolicy
{
    public DateTimeOffset OpensAt { get; }
    public DateTimeOffset ClosesAt { get; }

    /// <summary>
    /// Minimum time that must have elapsed since the later of an attendee's
    /// registration time and their last reconfirmation email before another
    /// reconfirmation email may be sent to them.
    /// </summary>
    public TimeSpan MinEmailInterval { get; }
    public TimeOnly? QuietHoursStart { get; }
    public TimeOnly? QuietHoursEnd { get; }

    private TicketedEventReconfirmPolicy(
        DateTimeOffset opensAt,
        DateTimeOffset closesAt,
        TimeSpan minEmailInterval,
        TimeOnly? quietHoursStart,
        TimeOnly? quietHoursEnd)
    {
        OpensAt = opensAt;
        ClosesAt = closesAt;
        MinEmailInterval = minEmailInterval;
        QuietHoursStart = quietHoursStart;
        QuietHoursEnd = quietHoursEnd;
    }

    public static TicketedEventReconfirmPolicy Create(
        DateTimeOffset opensAt,
        DateTimeOffset closesAt,
        TimeSpan minEmailInterval,
        TimeOnly? quietHoursStart = null,
        TimeOnly? quietHoursEnd = null)
    {
        if (closesAt <= opensAt)
            throw new BusinessRuleViolationException(Errors.WindowCloseBeforeOpen);

        if (minEmailInterval < TimeSpan.FromHours(1))
            throw new BusinessRuleViolationException(Errors.MinEmailIntervalBelowMinimum);

        if (minEmailInterval.Ticks % TimeSpan.TicksPerHour != 0)
            throw new BusinessRuleViolationException(Errors.MinEmailIntervalMustBeWholeHours);

        if (quietHoursStart.HasValue != quietHoursEnd.HasValue)
            throw new BusinessRuleViolationException(Errors.QuietHoursMustBePaired);

        if (quietHoursStart.HasValue && quietHoursStart == quietHoursEnd)
            throw new BusinessRuleViolationException(Errors.QuietHoursCannotBeEqual);

        return new TicketedEventReconfirmPolicy(
            opensAt,
            closesAt,
            minEmailInterval,
            quietHoursStart,
            quietHoursEnd);
    }

    internal static class Errors
    {
        public static readonly Error WindowCloseBeforeOpen = new(
            "ticketed_event_reconfirm_policy.window_close_before_open",
            "Reconfirmation window close time must be strictly after open time.");

        public static readonly Error MinEmailIntervalBelowMinimum = new(
            "ticketed_event_reconfirm_policy.min_email_interval_below_minimum",
            "Minimum email interval must be at least 1 hour.");

        public static readonly Error MinEmailIntervalMustBeWholeHours = new(
            "ticketed_event_reconfirm_policy.min_email_interval_must_be_whole_hours",
            "Minimum email interval must be a whole number of hours.");

        public static readonly Error QuietHoursMustBePaired = new(
            "ticketed_event_reconfirm_policy.quiet_hours_must_be_paired",
            "Quiet-hours start and end must be supplied together.");

        public static readonly Error QuietHoursCannotBeEqual = new(
            "ticketed_event_reconfirm_policy.quiet_hours_cannot_be_equal",
            "Quiet-hours start and end must be different.");
    }
}
