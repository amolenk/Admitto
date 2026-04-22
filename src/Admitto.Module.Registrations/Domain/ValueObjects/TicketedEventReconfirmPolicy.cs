using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

/// <summary>
/// Value-object policy describing the reconfirmation window and cadence for
/// a <c>TicketedEvent</c>. The cadence must be at least one day.
/// </summary>
public sealed record TicketedEventReconfirmPolicy
{
    public DateTimeOffset OpensAt { get; }
    public DateTimeOffset ClosesAt { get; }
    public TimeSpan Cadence { get; }

    private TicketedEventReconfirmPolicy(DateTimeOffset opensAt, DateTimeOffset closesAt, TimeSpan cadence)
    {
        OpensAt = opensAt;
        ClosesAt = closesAt;
        Cadence = cadence;
    }

    public static TicketedEventReconfirmPolicy Create(
        DateTimeOffset opensAt,
        DateTimeOffset closesAt,
        TimeSpan cadence)
    {
        if (closesAt <= opensAt)
            throw new BusinessRuleViolationException(Errors.WindowCloseBeforeOpen);

        if (cadence < TimeSpan.FromDays(1))
            throw new BusinessRuleViolationException(Errors.CadenceBelowMinimum);

        return new TicketedEventReconfirmPolicy(opensAt, closesAt, cadence);
    }

    internal static class Errors
    {
        public static readonly Error WindowCloseBeforeOpen = new(
            "ticketed_event_reconfirm_policy.window_close_before_open",
            "Reconfirmation window close time must be strictly after open time.");

        public static readonly Error CadenceBelowMinimum = new(
            "ticketed_event_reconfirm_policy.cadence_below_minimum",
            "Reconfirmation cadence must be at least 1 day.");
    }
}
