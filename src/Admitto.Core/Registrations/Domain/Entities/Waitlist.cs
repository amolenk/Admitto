using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

/// <summary>
/// Represents the waitlist for a single ticket type on an event.
/// Keyed by TicketTypeId (one Waitlist per ticket type).
/// </summary>
public class Waitlist : Aggregate<TicketTypeId>
{
    private readonly List<WaitlistEntry> _entries = [];
    private readonly List<WaitlistCoupon> _coupons = [];

    // Required for EF Core
    // ReSharper disable once UnusedMember.Local
    private Waitlist()
    {
    }

    private Waitlist(TicketedEventId eventId, TicketTypeId ticketTypeId, TeamId teamId)
        : base(ticketTypeId)
    {
        EventId = eventId;
        TeamId = teamId;
    }

    public TicketedEventId EventId { get; private set; }
    public TeamId TeamId { get; private set; }

    public int ActiveEntryCount => _entries.Count(e => e.Status == WaitlistEntryStatus.Active);

    public int IssuedCouponCount =>  _coupons.Count(e => e.Status == WaitlistCouponStatus.Issued);

    public IReadOnlyList<WaitlistEntry> Entries => _entries.AsReadOnly();
    public IReadOnlyList<WaitlistCoupon> Coupons => _coupons.AsReadOnly();

    public static Waitlist Create(TicketedEventId eventId, TicketTypeId ticketTypeId, TeamId teamId)
        => new(eventId, ticketTypeId, teamId);

    /// <summary>
    /// Adds an active waitlist entry immediately. Idempotent — returns false without adding a duplicate
    /// when the email already has an active entry.
    /// </summary>
    public bool AddEntry(EmailAddress email, DateTimeOffset addedAt)
    {
        if (_entries.Any(e => e.Email == email && e.Status == WaitlistEntryStatus.Active))
            return false;

        var nextPosition = _entries.Count(e => e.Status == WaitlistEntryStatus.Active) + 1;
        _entries.Add(new WaitlistEntry(WaitlistEntryId.New(), email, nextPosition, addedAt));
        return true;
    }

    /// <summary>
    /// Removes the active entry for the given email. Idempotent if not found.
    /// </summary>
    public void RemoveEntry(EmailAddress email)
    {
        var entry = _entries.FirstOrDefault(e => e.Email == email && e.Status == WaitlistEntryStatus.Active);
        if (entry is null)
            return;

        entry.Remove();
        RenumberPositions();
        AddDomainEvent(new WaitlistEntryRemovedDomainEvent(EventId, Id, entry.Id, email));
        CheckExhausted();
    }

    /// <summary>
    /// Removes the entry with the given ID. Idempotent if already removed.
    /// </summary>
    public void RemoveEntry(WaitlistEntryId entryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry is null)
            throw new BusinessRuleViolationException(Errors.EntryNotFound);

        if (entry.Status == WaitlistEntryStatus.Removed)
            return;

        entry.Remove();
        RenumberPositions();
        AddDomainEvent(new WaitlistEntryRemovedDomainEvent(EventId, Id, entry.Id, entry.Email));
        CheckExhausted();
    }

    // /// <summary>
    // /// Removes all active entries unconditionally (e.g., on force-disable of the waitlist).
    // /// </summary>
    // public void ForceRemoveAllEntries()
    // {
    //     foreach (var entry in _entries.Where(e => e.Status == WaitlistEntryStatus.Active).ToList())
    //     {
    //         entry.Remove();
    //     }
    //
    //     CheckExhausted();
    // }

    /// <summary>
    /// Issues a coupon to the top-ranked active waitlist entry and removes that entry from the.
    /// Returns <c>null</c> when there are no active entries.
    /// </summary>
    public Coupon? IssueNextCoupon(
        TicketedEvent ticketedEvent,
        TicketType ticketType,
        DateTimeOffset utcNow)
    {
        // TODO PopNextEntry
        var entry = _entries
            .Where(e => e.Status == WaitlistEntryStatus.Active)
            .MinBy(e => e.Position);

        if (entry is null)
            return null;

        entry.Remove();
        RenumberPositions();
        // END TODO

        var now = utcNow;
        var expiresAt = WaitlistClaimWindowCalculator.ComputeExpiresAt(
            now,
            ticketedEvent.TimeZone,
            ticketedEvent.QuietHoursStart,
            ticketedEvent.QuietHoursEnd,
            ticketType.ClaimWindowHours);

        var coupon = Coupon.Create(
            EventId,
            TeamId,
            entry.Email,
            [ticketType.Id],
            expiresAt,
            bypassRegistrationWindow: true,
            [new TicketTypeInfo(ticketType.Id)],
            now,
            CouponSource.Waitlist);

        _coupons.Add(new WaitlistCoupon(coupon.Id, now));

        // AddDomainEvent(new WaitlistCouponIssuedDomainEvent(
        //     TeamId, EventId, Id, entry.Email, couponCode, ticketTypeName, expiresAt));

        return coupon;
    }

    /// <summary>
    /// Tracks a coupon issued to an attendee from this waitlist.
    /// </summary>
    public void TrackIssuedCoupon(CouponId couponId, DateTimeOffset issuedAt)
    {
        _coupons.Add(new WaitlistCoupon(couponId, issuedAt));
    }

    /// <summary>
    /// Marks the given waitlist coupon as redeemed.
    /// </summary>
    public void RedeemCoupon(CouponId couponId)
    {
        var coupon = FindActiveCoupon(couponId);
        coupon.Redeem();
        CheckExhausted();
    }

    /// <summary>
    /// Marks the given waitlist coupon as revoked.
    /// </summary>
    public void RevokeCoupon(CouponId couponId)
    {
        var coupon = FindActiveCoupon(couponId);
        coupon.Revoke();
        CheckExhausted();
    }

    private WaitlistCoupon FindActiveCoupon(CouponId couponId)
    {
        var coupon = _coupons.FirstOrDefault(c => c.Id == couponId);
        if (coupon is null)
            throw new BusinessRuleViolationException(Errors.CouponNotFound);

        return coupon;
    }

    private void RenumberPositions()
    {
        var pos = 1;
        foreach (var entry in _entries
                     .Where(e => e.Status == WaitlistEntryStatus.Active)
                     .OrderBy(e => e.Position))
        {
            entry.UpdatePosition(pos++);
        }
    }

    private void CheckExhausted()
    {
        var hasActiveEntries = _entries.Any(e => e.Status == WaitlistEntryStatus.Active);
        var hasIssuedCoupons = _coupons.Any(c => c.Status == WaitlistCouponStatus.Issued);

        if (!hasActiveEntries && !hasIssuedCoupons)
            AddDomainEvent(new WaitlistExhaustedDomainEvent(EventId, Id));
    }

    internal static class Errors
    {
        public static readonly Error EntryNotFound = new(
            "waitlist.entry_not_found",
            "The waitlist entry could not be found.",
            Type: ErrorType.NotFound);

        public static readonly Error CouponNotFound = new(
            "waitlist.coupon_not_found",
            "The waitlist coupon could not be found.",
            Type: ErrorType.NotFound);
    }
}
