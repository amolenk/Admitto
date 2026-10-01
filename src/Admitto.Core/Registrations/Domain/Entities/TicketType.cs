using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

/// <summary>
/// A ticket type within a ticket catalog. Keyed by server-generated ID.
/// Combines ticket definition (name, time slots) with capacity tracking. <see cref="PublicCapacity"/> is the only
/// enforced limit; admin tickets (<see cref="ClaimMode.Admin"/>) come on top of it (see ADR-019).
/// </summary>
public class TicketType : Entity<TicketTypeId>
{
    private TicketType() { }

    internal TicketType(
        TicketTypeId id,
        TicketTypeName name,
        TimeSlot[] timeSlots,
        int? publicCapacity,
        bool selfServiceEnabled = true,
        bool waitlistEnabled = false,
        int claimWindowHours = 8,
        ReconfirmationEmailLimit? maxReconfirmationEmails = null)
        : base(id)
    {
        Name = name;
        TimeSlots = timeSlots;
        PublicCapacity = publicCapacity;
        PublicUsedCapacity = 0;
        SelfServiceEnabled = selfServiceEnabled;
        WaitlistEnabled = waitlistEnabled;
        ClaimWindowHours = claimWindowHours;
        UpdateMaxReconfirmationEmails(maxReconfirmationEmails);
    }

    public TicketTypeName Name { get; private set; }
    public TimeSlot[] TimeSlots { get; private set; } = [];

    /// <summary>
    /// Seats available through self-service; <c>null</c> means no limit. Admin registrations, organiser coupons and
    /// VIP promotions come on top of it and never use or free a public seat.
    /// </summary>
    public int? PublicCapacity { get; private set; }

    /// <summary>
    /// Tickets claimed from the public pool (<see cref="ClaimMode.Public"/>): self-service claims and redeemed
    /// automatic waitlist offers.
    /// </summary>
    public int PublicUsedCapacity { get; private set; }

    /// <summary>
    /// Admin tickets (<see cref="ClaimMode.Admin"/>) claimed on top of <see cref="PublicCapacity"/>. For information
    /// only; nothing enforces it.
    /// </summary>
    public int AdminUsedCount { get; private set; }

    public bool SelfServiceEnabled { get; private set; } = true;
    public bool WaitlistEnabled { get; private set; }
    public bool WaitlistMode { get; private set; }
    public int ClaimWindowHours { get; private set; } = 8;
    public ReconfirmationEmailLimit? MaxReconfirmationEmails { get; private set; }

    /// <summary>
    /// Public seats held by outstanding automatic waitlist offers. Taken when an automatic waitlist coupon is issued
    /// and given back when it lapses, or turned into a <see cref="ClaimMode.Public"/> claim when redeemed. VIP offers
    /// take no hold: they are admin tickets. Kept here rather than on the <see cref="Waitlist"/> so that every offer
    /// decision reads and writes one aggregate.
    /// </summary>
    public int WaitlistHeldCapacity { get; private set; }

    /// <summary>
    /// Attendees actively queued on this ticket type's waitlist. Updated in the same unit of work as every entry that
    /// joins or leaves the <see cref="Waitlist"/>, so that a WaitlistMode lift and a concurrent join write the same
    /// aggregate and cannot both commit.
    /// </summary>
    public int WaitlistQueuedCount { get; private set; }

    /// <summary>
    /// Public seats neither used nor held by an outstanding automatic waitlist offer. Unclamped: it only goes negative
    /// when <see cref="PublicCapacity"/> is lowered below what's committed, and that shortfall is made up by
    /// cancellations before anyone in the queue gets an offer. <c>null</c> when capacity is unbounded. Drives the
    /// waitlist offer decisions; public sales use <see cref="PublicAvailableCapacity"/>.
    /// </summary>
    public int? AvailableCapacity =>
        PublicCapacity - PublicUsedCapacity - WaitlistHeldCapacity;

    /// <summary>
    /// <see cref="AvailableCapacity"/> clamped at zero: the seats self-service can still claim. <c>null</c> when
    /// capacity is unbounded.
    /// </summary>
    public int? PublicAvailableCapacity => AvailableCapacity is int available ? Math.Max(0, available) : null;

    /// <summary>
    /// Whether the ticket type is sold out for self-service/public purposes, i.e. no public seats remain once the
    /// waitlist holds are taken into account. Does not gate admin claims.
    /// </summary>
    public bool IsSoldOut => PublicAvailableCapacity is 0;

    public void UpdateName(TicketTypeName name)
    {
        Name = name;
    }

    public void UpdateCapacity(int? publicCapacity)
    {
        PublicCapacity = publicCapacity;
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
    /// Holds a public seat for an automatic waitlist offer. Always allowed: the catalog only issues automatic offers
    /// while a seat is available.
    /// </summary>
    internal void HoldForWaitlistOffer()
    {
        WaitlistHeldCapacity++;
    }

    /// <summary>
    /// Gives back the seat held by an automatic waitlist offer that lapsed. Clamped at zero.
    /// </summary>
    internal void ReleaseWaitlistHold()
    {
        WaitlistHeldCapacity = Math.Max(0, WaitlistHeldCapacity - 1);
    }

    /// <summary>
    /// Counts an attendee joining the waitlist queue.
    /// </summary>
    internal void JoinWaitlistQueue()
    {
        WaitlistQueuedCount++;
    }

    /// <summary>
    /// Counts an attendee leaving the waitlist queue (removed, withdrawn, offered a coupon or redeemed one). Clamped
    /// at zero.
    /// </summary>
    internal void LeaveWaitlistQueue()
    {
        WaitlistQueuedCount = Math.Max(0, WaitlistQueuedCount - 1);
    }

    /// <summary>
    /// Claims one ticket under the given <see cref="ClaimMode"/>.
    /// <see cref="ClaimMode.Public"/> is enforced (throws if in WaitlistMode or sold out; self-service
    /// availability is checked upstream at catalog level) and increments <see cref="PublicUsedCapacity"/>.
    /// <see cref="ClaimMode.Admin"/> is never enforced, increments <see cref="AdminUsedCount"/> only and never touches
    /// <see cref="WaitlistHeldCapacity"/>.
    /// </summary>
    public void Claim(ClaimMode mode)
    {
        if (mode == ClaimMode.Admin)
        {
            AdminUsedCount++;
            return;
        }

        if (WaitlistMode)
            throw new BusinessRuleViolationException(Errors.TicketTypeInWaitlistMode(Id));

        if (IsSoldOut)
            throw new BusinessRuleViolationException(Errors.TicketTypeAtCapacity(Id));

        PublicUsedCapacity++;
    }

    /// <summary>
    /// Redeems an automatic waitlist offer: not enforced, it turns the offer's hold into a public ticket
    /// (<see cref="WaitlistHeldCapacity"/> −1, clamped at zero; <see cref="PublicUsedCapacity"/> +1).
    /// </summary>
    internal void ClaimWaitlistOffer()
    {
        PublicUsedCapacity++;
        ReleaseWaitlistHold();
    }

    /// <summary>
    /// Releases one ticket back to the pool it was claimed from, clamped at zero: <see cref="ClaimMode.Public"/>
    /// frees a public seat, <see cref="ClaimMode.Admin"/> only decrements <see cref="AdminUsedCount"/>.
    /// </summary>
    public void ReleaseCapacity(ClaimMode mode = ClaimMode.Public)
    {
        if (mode == ClaimMode.Admin)
            AdminUsedCount = Math.Max(0, AdminUsedCount - 1);
        else
            PublicUsedCapacity = Math.Max(0, PublicUsedCapacity - 1);
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
