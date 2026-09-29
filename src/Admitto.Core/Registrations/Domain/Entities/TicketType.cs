using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

/// <summary>
/// A ticket type within a ticket catalog. Keyed by server-generated ID.
/// Combines ticket definition (name, time slots) with capacity tracking (max, used).
/// </summary>
public class TicketType : Entity<TicketTypeId>
{
    private TicketType() { }

    internal TicketType(
        TicketTypeId id,
        TicketTypeName name,
        TimeSlot[] timeSlots,
        int? maxCapacity,
        bool selfServiceEnabled = true,
        bool waitlistEnabled = false,
        int claimWindowHours = 8,
        ReconfirmationEmailLimit? maxReconfirmationEmails = null,
        int reservedCapacity = 0)
        : base(id)
    {
        Name = name;
        TimeSlots = timeSlots;
        MaxCapacity = maxCapacity;
        UsedCapacity = 0;
        SelfServiceEnabled = selfServiceEnabled;
        WaitlistEnabled = waitlistEnabled;
        ClaimWindowHours = claimWindowHours;
        ReservedCapacity = reservedCapacity;
        UpdateMaxReconfirmationEmails(maxReconfirmationEmails);
    }

    public TicketTypeName Name { get; private set; }
    public TimeSlot[] TimeSlots { get; private set; } = [];
    public int? MaxCapacity { get; private set; }
    public int UsedCapacity { get; private set; }
    public bool SelfServiceEnabled { get; private set; } = true;
    public bool WaitlistEnabled { get; private set; }
    public bool WaitlistMode { get; private set; }
    public int ClaimWindowHours { get; private set; } = 8;
    public ReconfirmationEmailLimit? MaxReconfirmationEmails { get; private set; }

    /// <summary>
    /// Portion of <see cref="MaxCapacity"/> held back for admin/coupon (<see cref="ClaimMode.Reserved"/>)
    /// registrations. A sales restriction, not a separate pool with its own hard cap: once
    /// <see cref="ReservedUsedCapacity"/> reaches this value, further reserved claims spill into
    /// the public pool (admin/coupon claims remain uncapped by design).
    /// </summary>
    public int ReservedCapacity { get; private set; }

    /// <summary>
    /// Number of claims made with <see cref="ClaimMode.Reserved"/>. Unlike <see cref="ReservedCapacity"/>,
    /// this can exceed the reserved buffer — it simply means reserved claims have started consuming
    /// the public pool.
    /// </summary>
    public int ReservedUsedCapacity { get; private set; }

    /// <summary>
    /// Seats held by outstanding waitlist offers (automatic or VIP). Taken when a waitlist coupon is issued and
    /// given back when it lapses, or turned into a <see cref="ClaimMode.PublicUncapped"/> claim when redeemed. Kept
    /// here rather than on the <see cref="Waitlist"/> so that every offer decision reads and writes one aggregate.
    /// </summary>
    public int WaitlistHeldCapacity { get; private set; }

    /// <summary>
    /// Portion of <paramref name="reservedCapacity"/> not yet consumed by reserved claims, and
    /// therefore still held back from the public pool.
    /// </summary>
    private int HeldBack(int reservedCapacity) => Math.Max(0, reservedCapacity - ReservedUsedCapacity);

    /// <summary>
    /// Seats not used, held back for reserved claims, or held by outstanding waitlist offers. Unclamped: it goes
    /// negative while VIP offers are outstanding or organiser claims overbooked the ticket type, and that deficit is
    /// paid back before anyone else in the queue gets an offer. <c>null</c> when capacity is unbounded. Drives the
    /// waitlist offer decisions; public sales use <see cref="PublicAvailableCapacity"/>.
    /// </summary>
    public int? AvailableCapacity =>
        MaxCapacity is null ? null : UnclampedAvailableCapacity(MaxCapacity.Value, ReservedCapacity);

    private int UnclampedAvailableCapacity(int maxCapacity, int reservedCapacity) =>
        maxCapacity - UsedCapacity - HeldBack(reservedCapacity) - WaitlistHeldCapacity;

    /// <summary>
    /// Slots available to the public pool for the given max/reserved capacity, clamped at zero. Seats held by
    /// outstanding waitlist offers are not available to the public.
    /// </summary>
    public int PublicAvailableCapacity(int? maxCapacity, int reservedCapacity) =>
        Math.Max(0, UnclampedAvailableCapacity(maxCapacity ?? 0, reservedCapacity));

    /// <summary>
    /// Whether the ticket type is sold out for self-service/public purposes, i.e. no public slots
    /// remain once the unconsumed reserved buffer and the waitlist holds are held back. Does not gate
    /// admin/coupon claims.
    /// </summary>
    public bool IsSoldOut => MaxCapacity is not null && PublicAvailableCapacity(MaxCapacity, ReservedCapacity) <= 0;

    public void UpdateName(TicketTypeName name)
    {
        Name = name;
    }

    public void UpdateCapacity(int? maxCapacity)
    {
        MaxCapacity = maxCapacity;
    }

    public void UpdateReservedCapacity(int reservedCapacity)
    {
        ReservedCapacity = reservedCapacity;
    }

    public void UpdateSelfServiceEnabled(bool enabled)
    {
        SelfServiceEnabled = enabled;
    }

    public void UpdateMaxReconfirmationEmails(ReconfirmationEmailLimit? value)
    {
        MaxReconfirmationEmails = value;
    }

    internal void EnableWaitlist()
    {
        WaitlistEnabled = true;
    }

    internal void DisableWaitlist()
    {
        WaitlistEnabled = false;
    }

    internal void UpdateClaimWindowHours(int hours)
    {
        ClaimWindowHours = hours;
    }

    internal void ActivateWaitlistMode()
    {
        WaitlistMode = true;
    }

    internal void DeactivateWaitlistMode()
    {
        WaitlistMode = false;
    }

    /// <summary>
    /// Holds a seat for a waitlist offer. Always allowed, even with no seat available: a VIP offer made while sold
    /// out goes over, and the next seat that frees up covers it.
    /// </summary>
    internal void HoldForWaitlistOffer()
    {
        WaitlistHeldCapacity++;
    }

    /// <summary>
    /// Gives back the seat held by a waitlist offer that lapsed. Clamped at zero.
    /// </summary>
    internal void ReleaseWaitlistHold()
    {
        WaitlistHeldCapacity = Math.Max(0, WaitlistHeldCapacity - 1);
    }

    /// <summary>
    /// Claims one slot under the given <see cref="ClaimMode"/>.
    /// <see cref="ClaimMode.Public"/> is enforced (throws if in WaitlistMode or sold out; self-service
    /// availability is checked upstream at catalog level). <see cref="ClaimMode.PublicUncapped"/> and
    /// <see cref="ClaimMode.Reserved"/> are uncapped. <see cref="ClaimMode.PublicUncapped"/> redeems a waitlist
    /// offer, so it turns the offer's hold into the claim (<see cref="WaitlistHeldCapacity"/> −1, clamped at zero).
    /// Only <see cref="ClaimMode.Reserved"/> increments <see cref="ReservedUsedCapacity"/>.
    /// </summary>
    public void Claim(ClaimMode mode)
    {
        if (mode == ClaimMode.Public)
        {
            if (WaitlistMode)
                throw new BusinessRuleViolationException(Errors.TicketTypeInWaitlistMode(Id));

            if (IsSoldOut)
                throw new BusinessRuleViolationException(Errors.TicketTypeAtCapacity(Id));
        }

        UsedCapacity++;

        if (mode == ClaimMode.PublicUncapped)
            ReleaseWaitlistHold();

        if (mode == ClaimMode.Reserved)
            ReservedUsedCapacity++;
    }

    /// <summary>
    /// Decrements used capacity by 1, clamped at zero. When <paramref name="mode"/> is
    /// <see cref="ClaimMode.Reserved"/>, also decrements <see cref="ReservedUsedCapacity"/> (clamped at zero),
    /// crediting the reserved buffer back.
    /// </summary>
    public void ReleaseCapacity(ClaimMode mode = ClaimMode.Public)
    {
        UsedCapacity = Math.Max(0, UsedCapacity - 1);

        if (mode == ClaimMode.Reserved)
            ReservedUsedCapacity = Math.Max(0, ReservedUsedCapacity - 1);
    }

    internal static class Errors
    {
        public static Error TicketTypeNotAvailable(TicketTypeId id) =>
            new("ticket_type.not_available",
                "Ticket type is not available for self-service registration.",
                Details: new Dictionary<string, object?> { ["id"] = id.Value });

        public static Error TicketTypeInWaitlistMode(TicketTypeId id) =>
            new("ticket_type.waitlist_mode",
                "This ticket type is currently in waitlist mode.",
                Details: new Dictionary<string, object?> { ["id"] = id.Value });

        public static Error TicketTypeAtCapacity(TicketTypeId id) =>
            new("ticket_type.at_capacity",
                "Ticket type is at full capacity.",
                Details: new Dictionary<string, object?> { ["id"] = id.Value });

    }
}
