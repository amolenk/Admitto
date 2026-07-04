using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

/// <summary>
/// Value-object policy describing the reconfirmation window, cadence, and
/// per-attendee minimum email interval for a <c>TicketedEvent</c>.
/// The cadence and minimum email interval must each be at least one hour.
/// </summary>
public sealed record TicketedEventReconfirmPolicy
{
    public DateTimeOffset OpensAt { get; }
    public DateTimeOffset ClosesAt { get; }
    public TimeSpan Cadence { get; }

    /// <summary>
    /// Minimum time that must have elapsed since the later of an attendee's
    /// registration time and their last reconfirmation email before another
    /// reconfirmation email may be sent to them.
    /// </summary>
    public TimeSpan MinEmailInterval { get; }

    private TicketedEventReconfirmPolicy(
        DateTimeOffset opensAt,
        DateTimeOffset closesAt,
        TimeSpan cadence,
        TimeSpan minEmailInterval)
    {
        OpensAt = opensAt;
        ClosesAt = closesAt;
        Cadence = cadence;
        MinEmailInterval = minEmailInterval;
    }

    public static TicketedEventReconfirmPolicy Create(
        DateTimeOffset opensAt,
        DateTimeOffset closesAt,
        TimeSpan cadence,
        TimeSpan minEmailInterval)
    {
        if (closesAt <= opensAt)
            throw new BusinessRuleViolationException(Errors.WindowCloseBeforeOpen);

        if (cadence < TimeSpan.FromHours(1))
            throw new BusinessRuleViolationException(Errors.CadenceBelowMinimum);

        if (minEmailInterval < TimeSpan.FromHours(1))
            throw new BusinessRuleViolationException(Errors.MinEmailIntervalBelowMinimum);

        return new TicketedEventReconfirmPolicy(
            opensAt,
            closesAt,
            cadence,
            minEmailInterval);
    }

    internal static class Errors
    {
        public static readonly Error WindowCloseBeforeOpen = new(
            "ticketed_event_reconfirm_policy.window_close_before_open",
            "Reconfirmation window close time must be strictly after open time.");

        public static readonly Error CadenceBelowMinimum = new(
            "ticketed_event_reconfirm_policy.cadence_below_minimum",
            "Reconfirmation cadence must be at least 1 hour.");

        public static readonly Error MinEmailIntervalBelowMinimum = new(
            "ticketed_event_reconfirm_policy.min_email_interval_below_minimum",
            "Minimum email interval must be at least 1 hour.");
    }
}
