using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
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

    /// <summary>
    /// Returns whether the given email currently holds an outstanding, unredeemed waitlist offer on this
    /// waitlist (an <see cref="WaitlistEntryStatus.Offered"/> entry).
    /// </summary>
    public bool HasOfferedEntry(EmailAddress email)
        => _entries.Any(e => e.Email == email && e.Status == WaitlistEntryStatus.Offered);

    /// <summary>
    /// Returns the outstanding offer (coupon id and its tracked expiry) for the given email, if any.
    /// </summary>
    public (CouponId CouponId, DateTimeOffset ExpiresAt)? GetOfferedEntry(EmailAddress email)
    {
        var entry = _entries.FirstOrDefault(e => e.Email == email && e.Status == WaitlistEntryStatus.Offered);
        if (entry?.CouponId is not { } couponId)
            return null;

        var waitlistCoupon = _coupons.FirstOrDefault(c => c.Id == couponId);
        return waitlistCoupon is null ? null : (couponId, waitlistCoupon.ExpiresAt);
    }

    public static Waitlist Create(TicketedEventId eventId, TicketTypeId ticketTypeId, TeamId teamId)
        => new(eventId, ticketTypeId, teamId);

    /// <summary>
    /// Adds an active waitlist entry immediately and counts it on the <paramref name="catalog"/>'s ticket type.
    /// Idempotent — returns false without adding a duplicate when the email already holds an active entry or an
    /// outstanding offer (an <see cref="WaitlistEntryStatus.Offered"/> entry is still a current selection).
    /// </summary>
    public bool AddEntry(EmailAddress email, DateTimeOffset addedAt, TicketCatalog catalog, RegistrationId registrationId)
    {
        if (_entries.Any(e => e.Email == email && e.Status != WaitlistEntryStatus.Removed))
            return false;

        catalog.JoinWaitlistQueue(Id);

        var nextPosition = _entries.Count(e => e.Status == WaitlistEntryStatus.Active) + 1;
        _entries.Add(new WaitlistEntry(WaitlistEntryId.New(), email, nextPosition, addedAt, registrationId));
        return true;
    }

    /// <summary>
    /// Leaves the queue or withdraws the outstanding offer for the given email, whichever applies, and its count
    /// on the <paramref name="catalog"/>'s ticket type. An outstanding offer's <see cref="WaitlistCoupon"/> is
    /// expired and its hold (if automatic) released, same as <see cref="WithdrawCoupon"/>. Idempotent if not
    /// found. Returns the id of the coupon whose offer was withdrawn, if any, so the caller can also expire the
    /// matching <see cref="Coupon"/> aggregate.
    /// </summary>
    public CouponId? RemoveEntry(EmailAddress email, TicketCatalog catalog)
    {
        var entry = _entries.FirstOrDefault(e => e.Email == email && e.Status != WaitlistEntryStatus.Removed);
        if (entry is null)
            return null;

        var withdrawnCouponId = RemoveEntryCore(entry, catalog);
        CheckExhausted();
        return withdrawnCouponId;
    }

    /// <summary>
    /// Removes the entry with the given ID — whether still queued or holding an outstanding offer — and its count
    /// on the <paramref name="catalog"/>'s ticket type. Idempotent if already removed. Returns the id of the
    /// coupon whose offer was withdrawn, if any, so the caller can also expire the matching <see cref="Coupon"/>
    /// aggregate.
    /// </summary>
    public CouponId? RemoveEntry(WaitlistEntryId entryId, TicketCatalog catalog)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry is null)
            throw new BusinessRuleViolationException(Errors.EntryNotFound);

        if (entry.Status == WaitlistEntryStatus.Removed)
            return null;

        var withdrawnCouponId = RemoveEntryCore(entry, catalog);
        CheckExhausted();
        return withdrawnCouponId;
    }

    /// <summary>
    /// Takes an entry out of the waitlist, whatever its current status, raising
    /// <see cref="WaitlistEntryRemovedDomainEvent"/>. An <see cref="WaitlistEntryStatus.Active"/> entry leaves the
    /// queue and is renumbered; an <see cref="WaitlistEntryStatus.Offered"/> entry has its outstanding
    /// <see cref="WaitlistCoupon"/> expired and its hold (if automatic) released. Returns the withdrawn coupon's
    /// id when the entry held one.
    /// </summary>
    private CouponId? RemoveEntryCore(WaitlistEntry entry, TicketCatalog catalog)
    {
        CouponId? withdrawnCouponId = null;

        if (entry.Status == WaitlistEntryStatus.Active)
        {
            LeaveQueue(entry, catalog);
            RenumberPositions();
        }
        else if (entry.Status == WaitlistEntryStatus.Offered)
        {
            withdrawnCouponId = entry.CouponId;
            if (withdrawnCouponId is { } couponId
                && _coupons.FirstOrDefault(c => c.Id == couponId) is { Status: WaitlistCouponStatus.Issued } waitlistCoupon)
            {
                ExpireAndReleaseHold(waitlistCoupon, catalog);
            }
        }

        entry.Remove();
        AddDomainEvent(new WaitlistEntryRemovedDomainEvent(TeamId, EventId, Id, entry.Id, entry.Email));
        return withdrawnCouponId;
    }

    /// <summary>
    /// Handles an explicit disable of this waitlist's ticket type: offers up to <paramref name="freedSlots"/>
    /// coupons to the front of the queue, then removes everyone still waiting. Removed attendees get no email
    /// (the organizer informs them); outstanding coupons stay valid, and keep their hold, until they are redeemed
    /// or expire. Returns the newly issued coupons.
    /// </summary>
    public IReadOnlyList<Coupon> Disable(
        int freedSlots,
        TicketedEvent ticketedEvent,
        TicketCatalog catalog,
        DateTimeOffset utcNow)
    {
        var coupons = IssueNextCoupons(
            freedSlots, ticketedEvent, catalog, utcNow, WaitlistOfferReason.AutomaticPromotion);

        var remainingEntries = _entries.Where(e => e.Status == WaitlistEntryStatus.Active).ToList();
        foreach (var entry in remainingEntries)
        {
            LeaveQueue(entry, catalog);
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
        TicketCatalog catalog,
        DateTimeOffset utcNow)
        => IssueNextCoupons(
            ActiveEntryCount, ticketedEvent, catalog, utcNow, WaitlistOfferReason.CapacityOpenedForEveryone);

    private List<Coupon> IssueNextCoupons(
        int maxCount,
        TicketedEvent ticketedEvent,
        TicketCatalog catalog,
        DateTimeOffset utcNow,
        WaitlistOfferReason reason)
    {
        var coupons = new List<Coupon>();
        while (coupons.Count < maxCount
               && IssueNextCouponCore(ticketedEvent, catalog, utcNow, reason) is { } coupon)
            coupons.Add(coupon);

        return coupons;
    }

    /// <summary>
    /// Issues a coupon to the top-ranked active waitlist entry and removes that entry from the queue. The offer
    /// holds a public seat on the <paramref name="catalog"/>'s ticket type, and the entry leaves its queued count.
    /// Returns <c>null</c> when there are no active entries.
    /// </summary>
    public Coupon? IssueNextCoupon(
        TicketedEvent ticketedEvent,
        TicketCatalog catalog,
        DateTimeOffset utcNow)
        => IssueNextCouponCore(ticketedEvent, catalog, utcNow, WaitlistOfferReason.AutomaticPromotion);

    private Coupon? IssueNextCouponCore(
        TicketedEvent ticketedEvent,
        TicketCatalog catalog,
        DateTimeOffset utcNow,
        WaitlistOfferReason reason)
    {
        var entry = _entries
            .Where(e => e.Status == WaitlistEntryStatus.Active)
            .MinBy(e => e.Position);

        return entry is null
            ? null
            : IssueCoupon(
                entry, ticketedEvent, catalog, utcNow, WaitlistCouponOrigin.Automatic, reason, entry.RegistrationId);
    }

    /// <summary>
    /// Issues a coupon to one specific active waitlist entry, regardless of its queue position (e.g. a VIP
    /// promotion by an organizer), and removes that entry from the queue. The offer takes no public hold: redeemed, it
    /// is an admin ticket on top of public capacity.
    /// </summary>
    public Coupon IssueCouponToEntry(
        WaitlistEntryId entryId,
        TicketedEvent ticketedEvent,
        TicketCatalog catalog,
        DateTimeOffset utcNow)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId && e.Status == WaitlistEntryStatus.Active);
        if (entry is null)
            throw new BusinessRuleViolationException(Errors.EntryNotActive);

        return IssueCoupon(
            entry, ticketedEvent, catalog, utcNow, WaitlistCouponOrigin.Manual, WaitlistOfferReason.VipPromotion,
            entry.RegistrationId);
    }

    private Coupon IssueCoupon(
        WaitlistEntry entry,
        TicketedEvent ticketedEvent,
        TicketCatalog catalog,
        DateTimeOffset utcNow,
        WaitlistCouponOrigin origin,
        WaitlistOfferReason reason,
        RegistrationId registrationId)
    {
        var ticketType = catalog.FindTicketType(Id);
        if (origin == WaitlistCouponOrigin.Automatic)
            catalog.HoldForWaitlistOffer(Id);

        var expiresAt = WaitlistClaimWindowCalculator.ComputeExpiresAt(
            utcNow,
            ticketedEvent.TimeZone,
            ticketedEvent.WaitlistPolicy.QuietHoursStart,
            ticketedEvent.WaitlistPolicy.QuietHoursEnd,
            ticketType.ClaimWindowHours,
            ticketedEvent.StartsAt);

        var coupon = Coupon.Create(
            EventId,
            TeamId,
            entry.Email,
            [ticketType.Id],
            expiresAt,
            bypassRegistrationWindow: true,
            [new TicketTypeInfo(ticketType.Id)],
            utcNow,
            CouponSource.Waitlist,
            origin);

        _coupons.Add(new WaitlistCoupon(coupon.Id, utcNow, expiresAt, origin));

        // The entry leaves the queue (and its count) but is not removed: it becomes Offered, still a current
        // selection on the registration, until the offer is redeemed, withdrawn, or expires.
        entry.Offer(coupon.Id);
        catalog.LeaveWaitlistQueue(Id);
        RenumberPositions();

        AddDomainEvent(new WaitlistCouponIssuedDomainEvent(
            TeamId, EventId, ticketType.Id, entry.Email, coupon.Code, ticketType.Name.Value, expiresAt, reason,
            registrationId));

        return coupon;
    }

    /// <summary>
    /// Applies a coupon redemption that granted this waitlist's ticket type, whatever the coupon's source: removes
    /// the redeeming email's entry (whether still queued or holding this/another outstanding offer) and its count
    /// on the <paramref name="catalog"/>'s ticket type where applicable, and marks the coupon redeemed if it was
    /// issued from this waitlist.
    /// </summary>
    public void ApplyCouponRedemption(CouponId couponId, EmailAddress email, TicketCatalog catalog)
    {
        var entry = _entries.FirstOrDefault(e => e.Email == email && e.Status != WaitlistEntryStatus.Removed);
        var entryRemoved = entry is not null;
        if (entry is not null)
        {
            if (entry.Status == WaitlistEntryStatus.Active)
            {
                LeaveQueue(entry, catalog);
                RenumberPositions();
            }
            else
            {
                // Offered: the entry already left the queue at offer issuance, so there's nothing further
                // to release here beyond the entry itself.
                entry.Remove();
            }

            AddDomainEvent(new WaitlistEntryRemovedDomainEvent(TeamId, EventId, Id, entry.Id, email));
        }

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
    /// Marks the given waitlist coupon as expired because it lapsed unclaimed, removes the <see cref="Offered"/>
    /// entry that held it, gives back the seat an automatic offer held on the <paramref name="catalog"/> (which
    /// decides whether that leaves a seat for the next person waiting; a VIP offer held none, so its lapse offers
    /// nobody a seat), and raises <see cref="WaitlistCouponExpiredDomainEvent"/> so its recipient is told the offer
    /// expired. The <paramref name="coupon"/> only supplies that email's recipient and code; when it or the ticket
    /// type no longer exists there is nothing to send, so the coupon is expired without raising the event. Returns
    /// the removed entry, if its email/registration are still needed by the caller (e.g. to check whether the
    /// registration now has no selection left at all).
    /// </summary>
    /// <remarks>
    /// <paramref name="registrationClosed"/> tells the recipient's email not to invite them to register again.
    /// </remarks>
    public WaitlistEntry? ExpireCoupon(CouponId couponId, Coupon? coupon, TicketCatalog catalog, bool registrationClosed)
    {
        var waitlistCoupon = FindCoupon(couponId);
        ExpireAndReleaseHold(waitlistCoupon, catalog);

        var entry = RemoveEntryForCoupon(couponId);

        var ticketType = catalog.GetTicketType(Id);
        if (coupon is not null && ticketType is not null)
        {
            AddDomainEvent(new WaitlistCouponExpiredDomainEvent(
                TeamId, EventId, ticketType.Id, coupon.Email, coupon.Code, ticketType.Name.Value, registrationClosed));
        }

        CheckExhausted();
        return entry;
    }

    /// <summary>
    /// Withdraws an outstanding offer without telling its recipient, e.g. because an admin registered them for this
    /// ticket type, or the registration holding it was cancelled: the coupon expires, the <see cref="Offered"/>
    /// entry is removed, and an automatic offer gives back its hold on the <paramref name="catalog"/>, so the seat
    /// can go to the next person waiting. Returns <c>false</c> when the coupon is not outstanding.
    /// </summary>
    public bool WithdrawCoupon(CouponId couponId, TicketCatalog catalog)
    {
        var waitlistCoupon = _coupons.FirstOrDefault(c => c.Id == couponId);
        if (waitlistCoupon?.Status != WaitlistCouponStatus.Issued)
            return false;

        ExpireAndReleaseHold(waitlistCoupon, catalog);
        RemoveEntryForCoupon(couponId);
        CheckExhausted();
        return true;
    }

    /// <summary>
    /// Removes the <see cref="WaitlistEntryStatus.Offered"/> entry holding the given coupon, if any, raising
    /// <see cref="WaitlistEntryRemovedDomainEvent"/>. No queue count is touched: an offered entry already left
    /// the queue when the offer was issued.
    /// </summary>
    private WaitlistEntry? RemoveEntryForCoupon(CouponId couponId)
    {
        var entry = _entries.FirstOrDefault(
            e => e.CouponId == couponId && e.Status == WaitlistEntryStatus.Offered);
        if (entry is null)
            return null;

        entry.Remove();
        AddDomainEvent(new WaitlistEntryRemovedDomainEvent(TeamId, EventId, Id, entry.Id, entry.Email));
        return entry;
    }

    private void ExpireAndReleaseHold(WaitlistCoupon waitlistCoupon, TicketCatalog catalog)
    {
        waitlistCoupon.Expire();
        if (waitlistCoupon.Origin == WaitlistCouponOrigin.Automatic)
            catalog.ReleaseWaitlistHold(Id);
    }

    private WaitlistCoupon FindCoupon(CouponId couponId)
    {
        var coupon = _coupons.FirstOrDefault(c => c.Id == couponId);
        if (coupon is null)
            throw new BusinessRuleViolationException(Errors.CouponNotFound);

        return coupon;
    }

    /// <summary>
    /// Takes the entry out of the queue and out of the <paramref name="catalog"/>'s queued count together, keeping
    /// <see cref="TicketType.WaitlistQueuedCount"/> equal to the number of active entries.
    /// </summary>
    private void LeaveQueue(WaitlistEntry entry, TicketCatalog catalog)
    {
        entry.Remove();
        catalog.LeaveWaitlistQueue(Id);
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
