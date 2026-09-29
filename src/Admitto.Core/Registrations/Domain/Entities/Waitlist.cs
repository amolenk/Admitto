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

    /// <summary>
    /// Returns the queue position of the active entry for the given email, if any.
    /// </summary>
    public int? GetActivePosition(EmailAddress email)
        => _entries
            .Where(e => e.Email == email && e.Status == WaitlistEntryStatus.Active)
            .Select(e => (int?)e.Position)
            .FirstOrDefault();

    /// <summary>
    /// Returns whether the given email currently holds an active entry on this waitlist.
    /// </summary>
    public bool HasActiveEntry(EmailAddress email)
        => _entries.Any(e => e.Email == email && e.Status == WaitlistEntryStatus.Active);

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
        if (RemoveActiveEntry(email))
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
        AddDomainEvent(new WaitlistEntryRemovedDomainEvent(TeamId, EventId, Id, entry.Id, entry.Email));
        CheckExhausted();
    }

    /// <summary>
    /// Handles an explicit disable of this waitlist's ticket type: offers up to <paramref name="freedSlots"/>
    /// coupons to the front of the queue, then removes everyone still waiting. Removed attendees get no email
    /// (the organizer informs them); outstanding coupons stay valid until they are redeemed or expire.
    /// Returns the newly issued coupons.
    /// </summary>
    public IReadOnlyList<Coupon> Disable(
        int freedSlots,
        TicketedEvent ticketedEvent,
        TicketType ticketType,
        DateTimeOffset utcNow)
    {
        var coupons = IssueNextCoupons(freedSlots, ticketedEvent, ticketType, utcNow);

        var remainingEntries = _entries.Where(e => e.Status == WaitlistEntryStatus.Active).ToList();
        foreach (var entry in remainingEntries)
        {
            entry.Remove();
            AddDomainEvent(new WaitlistEntryRemovedDomainEvent(TeamId, EventId, Id, entry.Id, entry.Email));
        }

        if (remainingEntries.Count > 0)
            CheckExhausted();

        return coupons;
    }

    /// <summary>
    /// Issues a coupon to every active entry in queue order, e.g. when the ticket type's capacity limit is
    /// removed and there is room for everyone. Returns the issued coupons.
    /// </summary>
    public IReadOnlyList<Coupon> IssueCouponsToAllEntries(
        TicketedEvent ticketedEvent,
        TicketType ticketType,
        DateTimeOffset utcNow)
        => IssueNextCoupons(ActiveEntryCount, ticketedEvent, ticketType, utcNow);

    private List<Coupon> IssueNextCoupons(
        int maxCount,
        TicketedEvent ticketedEvent,
        TicketType ticketType,
        DateTimeOffset utcNow)
    {
        var coupons = new List<Coupon>();
        while (coupons.Count < maxCount && IssueNextCoupon(ticketedEvent, ticketType, utcNow) is { } coupon)
            coupons.Add(coupon);

        return coupons;
    }

    /// <summary>
    /// Issues a coupon to the top-ranked active waitlist entry and removes that entry from the queue.
    /// Returns <c>null</c> when there are no active entries.
    /// </summary>
    public Coupon? IssueNextCoupon(
        TicketedEvent ticketedEvent,
        TicketType ticketType,
        DateTimeOffset utcNow)
    {
        var entry = _entries
            .Where(e => e.Status == WaitlistEntryStatus.Active)
            .MinBy(e => e.Position);

        return entry is null
            ? null
            : IssueCoupon(entry, ticketedEvent, ticketType, utcNow, WaitlistCouponOrigin.Automatic);
    }

    /// <summary>
    /// Issues a coupon to one specific active waitlist entry, regardless of its queue position (e.g. a VIP
    /// promotion by an organizer), and removes that entry from the queue.
    /// </summary>
    public Coupon IssueCouponToEntry(
        WaitlistEntryId entryId,
        TicketedEvent ticketedEvent,
        TicketType ticketType,
        DateTimeOffset utcNow)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId && e.Status == WaitlistEntryStatus.Active);
        if (entry is null)
            throw new BusinessRuleViolationException(Errors.EntryNotActive);

        return IssueCoupon(entry, ticketedEvent, ticketType, utcNow, WaitlistCouponOrigin.Manual);
    }

    private Coupon IssueCoupon(
        WaitlistEntry entry,
        TicketedEvent ticketedEvent,
        TicketType ticketType,
        DateTimeOffset utcNow,
        WaitlistCouponOrigin origin)
    {
        entry.Remove();
        RenumberPositions();

        var expiresAt = WaitlistClaimWindowCalculator.ComputeExpiresAt(
            utcNow,
            ticketedEvent.TimeZone,
            ticketedEvent.WaitlistPolicy.QuietHoursStart,
            ticketedEvent.WaitlistPolicy.QuietHoursEnd,
            ticketType.ClaimWindowHours);

        var coupon = Coupon.Create(
            EventId,
            TeamId,
            entry.Email,
            [ticketType.Id],
            expiresAt,
            bypassRegistrationWindow: true,
            [new TicketTypeInfo(ticketType.Id)],
            utcNow,
            CouponSource.Waitlist);

        _coupons.Add(new WaitlistCoupon(coupon.Id, utcNow, expiresAt, origin));

        AddDomainEvent(new WaitlistCouponIssuedDomainEvent(
            TeamId, EventId, ticketType.Id, entry.Email, coupon.Code, ticketType.Name.Value, expiresAt));

        return coupon;
    }

    /// <summary>
    /// Applies a coupon redemption that granted this waitlist's ticket type, whatever the coupon's source:
    /// removes the redeeming email's active entry (if any), and marks the coupon redeemed if it was issued
    /// from this waitlist.
    /// </summary>
    public void ApplyCouponRedemption(CouponId couponId, EmailAddress email)
    {
        var entryRemoved = RemoveActiveEntry(email);

        var issuedCoupon = _coupons.FirstOrDefault(c => c.Id == couponId);
        issuedCoupon?.Redeem();

        if (entryRemoved || issuedCoupon is not null)
            CheckExhausted();
    }

    /// <summary>
    /// Returns the ids of the issued coupons whose offer lapsed at or before <paramref name="cutoff"/>.
    /// </summary>
    public IReadOnlyList<CouponId> GetLapsedCouponIds(DateTimeOffset cutoff)
        => _coupons
            .Where(c => c.Status == WaitlistCouponStatus.Issued && c.ExpiresAt <= cutoff)
            .Select(c => c.Id)
            .ToList();

    /// <summary>
    /// Marks the given waitlist coupon as expired because it lapsed unclaimed, and raises
    /// <see cref="WaitlistCouponExpiredDomainEvent"/> so its recipient is told the offer expired. The
    /// <paramref name="coupon"/> only supplies that email's recipient and code; when it or the ticket type
    /// no longer exists there is nothing to send, so the coupon is expired without raising the event.
    /// Returns whether the coupon was backed by a freed slot (<see cref="WaitlistCouponOrigin.Automatic"/>),
    /// which the caller should cascade to the next entry; a manually issued (VIP) coupon frees nothing.
    /// </summary>
    public bool ExpireCoupon(CouponId couponId, Coupon? coupon, TicketType? ticketType)
    {
        var waitlistCoupon = FindCoupon(couponId);
        waitlistCoupon.Expire();

        if (coupon is not null && ticketType is not null)
        {
            AddDomainEvent(new WaitlistCouponExpiredDomainEvent(
                TeamId, EventId, ticketType.Id, coupon.Email, coupon.Code, ticketType.Name.Value));
        }

        CheckExhausted();
        return waitlistCoupon.Origin == WaitlistCouponOrigin.Automatic;
    }

    private WaitlistCoupon FindCoupon(CouponId couponId)
    {
        var coupon = _coupons.FirstOrDefault(c => c.Id == couponId);
        if (coupon is null)
            throw new BusinessRuleViolationException(Errors.CouponNotFound);

        return coupon;
    }

    private bool RemoveActiveEntry(EmailAddress email)
    {
        var entry = _entries.FirstOrDefault(e => e.Email == email && e.Status == WaitlistEntryStatus.Active);
        if (entry is null)
            return false;

        entry.Remove();
        RenumberPositions();
        AddDomainEvent(new WaitlistEntryRemovedDomainEvent(TeamId, EventId, Id, entry.Id, email));
        return true;
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
            AddDomainEvent(new WaitlistExhaustedDomainEvent(TeamId, EventId, Id));
    }

    internal static class Errors
    {
        public static readonly Error EntryNotFound = new(
            "waitlist.entry_not_found",
            "The waitlist entry could not be found.",
            Type: ErrorType.NotFound);

        public static readonly Error EntryNotActive = new(
            "waitlist.entry_not_active",
            "The waitlist entry is no longer active on this waitlist.",
            Type: ErrorType.Conflict);

        public static readonly Error CouponNotFound = new(
            "waitlist.coupon_not_found",
            "The waitlist coupon could not be found.",
            Type: ErrorType.NotFound);
    }
}
