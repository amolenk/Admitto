using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.Entities;

/// <summary>
/// Per-event policy describing the reconfirmation window and cadence.
/// Optional — absence means attendees are never asked to reconfirm.
/// </summary>
public class ReconfirmPolicy : Aggregate<TicketedEventId>
{
    private ReconfirmPolicy() { }

    private ReconfirmPolicy(
        TicketedEventId id,
        DateTimeOffset opensAt,
        DateTimeOffset closesAt,
        TimeSpan cadence)
        : base(id)
    {
        Validate(opensAt, closesAt, cadence);
        OpensAt = opensAt;
        ClosesAt = closesAt;
        Cadence = cadence;
    }

    public DateTimeOffset OpensAt { get; private set; }
    public DateTimeOffset ClosesAt { get; private set; }
    public TimeSpan Cadence { get; private set; }

    public static ReconfirmPolicy Create(
        TicketedEventId eventId,
        DateTimeOffset opensAt,
        DateTimeOffset closesAt,
        TimeSpan cadence)
        => new(eventId, opensAt, closesAt, cadence);

    public void Update(DateTimeOffset opensAt, DateTimeOffset closesAt, TimeSpan cadence)
    {
        Validate(opensAt, closesAt, cadence);
        OpensAt = opensAt;
        ClosesAt = closesAt;
        Cadence = cadence;
    }

    private static void Validate(DateTimeOffset opensAt, DateTimeOffset closesAt, TimeSpan cadence)
    {
        if (closesAt <= opensAt)
            throw new BusinessRuleViolationException(Errors.WindowCloseBeforeOpen);

        if (cadence < TimeSpan.FromDays(1))
            throw new BusinessRuleViolationException(Errors.CadenceBelowMinimum);
    }

    internal static class Errors
    {
        public static readonly Error WindowCloseBeforeOpen = new(
            "reconfirm_policy.window_close_before_open",
            "Reconfirmation window close time must be after open time.");

        public static readonly Error CadenceBelowMinimum = new(
            "reconfirm_policy.cadence_below_minimum",
            "Reconfirmation cadence must be at least 1 day.");
    }
}
