using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

/// <summary>
/// Represents a single-use invitation to register for an event with specific ticket types.
/// Coupons bypass capacity and email domain restrictions. They optionally bypass the registration window.
/// </summary>
public class Coupon : Aggregate<CouponId>
{
    private readonly List<TicketTypeId> _allowedTicketTypeIds = [];

    // Required for EF Core
    // ReSharper disable once UnusedMember.Local
    private Coupon()
    {
    }

    private Coupon(
        CouponId id,
        TicketedEventId eventId,
        TeamId teamId,
        CouponCode code,
        EmailAddress email,
        IReadOnlyList<TicketTypeId> allowedTicketTypeIds,
        DateTimeOffset expiresAt,
        bool bypassRegistrationWindow,
        CouponSource source,
        WaitlistCouponOrigin? waitlistOrigin)
        : base(id)
    {
        EventId = eventId;
        TeamId = teamId;
        Code = code;
        Email = email;
        ExpiresAt = expiresAt;
        BypassRegistrationWindow = bypassRegistrationWindow;
        Source = source;
        WaitlistOrigin = waitlistOrigin;

        _allowedTicketTypeIds = allowedTicketTypeIds.ToList();
    }

    public TicketedEventId EventId { get; private set; }
    public TeamId TeamId { get; private set; }
    public CouponCode Code { get; private set; }
    public EmailAddress Email { get; private set; }
    public IReadOnlyList<TicketTypeId> AllowedTicketTypeIds => _allowedTicketTypeIds.AsReadOnly();
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool BypassRegistrationWindow { get; private set; }
    public CouponSource Source { get; private set; }

    /// <summary>
    /// How a waitlist coupon was issued (automatic front-of-queue offer or VIP promotion); <c>null</c> for organiser
    /// coupons. Decides the pool a redemption claims from (<see cref="RedemptionClaimMode"/>).
    /// </summary>
    public WaitlistCouponOrigin? WaitlistOrigin { get; private set; }
    public DateTimeOffset? RedeemedAt { get; private set; }

    public CouponStatus GetStatus(DateTimeOffset now)
    {
        if (RedeemedAt.HasValue) return CouponStatus.Redeemed;
        if (ExpiresAt <= now) return CouponStatus.Expired;
        return CouponStatus.Active;
    }

    public static Coupon Create(
        TicketedEventId eventId,
        TeamId teamId,
        EmailAddress email,
        IReadOnlyList<TicketTypeId> requestedTicketTypeIds,
        DateTimeOffset expiresAt,
        bool bypassRegistrationWindow,
        IReadOnlyList<TicketTypeInfo> availableTicketTypes,
        DateTimeOffset now,
        CouponSource source = CouponSource.Organiser,
        WaitlistCouponOrigin? waitlistOrigin = null)
    {
        // Validate at least one ticket type.
        if (requestedTicketTypeIds.Count == 0)
        {
            throw new BusinessRuleViolationException(Errors.NoTicketTypes);
        }

        // Validate all requested ticket types exist.
        var availableLookup = availableTicketTypes.ToDictionary(t => t.Id);
        var unknownIds = requestedTicketTypeIds
            .Where(id => !availableLookup.ContainsKey(id))
            .Select(id => id.Value)
            .ToList();

        if (unknownIds.Count > 0)
        {
            throw new BusinessRuleViolationException(Errors.UnknownTicketTypes(unknownIds));
        }

        // Validate expiry is in the future.
        if (expiresAt <= now)
        {
            throw new BusinessRuleViolationException(Errors.ExpiryMustBeInFuture);
        }

        var coupon = new Coupon(
            CouponId.New(),
            eventId,
            teamId,
            CouponCode.New(),
            email,
            requestedTicketTypeIds,
            expiresAt,
            bypassRegistrationWindow,
            source,
            source == CouponSource.Waitlist ? waitlistOrigin ?? WaitlistCouponOrigin.Automatic : null);

        if (source == CouponSource.Organiser)
        {
            coupon.AddDomainEvent(new CouponCreatedDomainEvent(
                coupon.Id,
                coupon.TeamId,
                coupon.EventId,
                coupon.Email,
                coupon.Code));
        }

        return coupon;
    }

    /// <summary>
    /// The capacity pool a redemption of this coupon claims from. This is capacity bookkeeping only, not a
    /// redemption rule: an automatic waitlist offer converts the public seat it holds into a public ticket, while
    /// an organiser coupon or a VIP offer claims admin tickets on top of public capacity (see <see cref="ClaimMode"/>).
    /// </summary>
    public ClaimMode RedemptionClaimMode =>
        Source == CouponSource.Waitlist && WaitlistOrigin == WaitlistCouponOrigin.Automatic
            ? ClaimMode.Public
            : ClaimMode.Admin;

    /// <summary>
    /// Redeems the coupon against the ticket types the attendee is claiming, regardless of the coupon's source.
    /// Succeeds as long as the selection includes at least one of the coupon's allowed ticket types; allowed
    /// ticket types missing from the selection are forfeited and the coupon is still fully redeemed
    /// (single-use). Returns the ticket types actually granted by this redemption.
    /// </summary>
    public IReadOnlyList<TicketTypeId> Redeem(
        EmailAddress email,
        IReadOnlyList<TicketTypeId> selectedTicketTypeIds,
        DateTimeOffset now)
    {
        var status = GetStatus(now);
        if (status == CouponStatus.Expired)
            throw new BusinessRuleViolationException(Errors.Expired);
        if (status == CouponStatus.Redeemed)
            throw new BusinessRuleViolationException(Errors.AlreadyRedeemed);

        if (Email != email)
            throw new BusinessRuleViolationException(Errors.EmailMismatch);

        var granted = _allowedTicketTypeIds
            .Where(selectedTicketTypeIds.Contains)
            .ToList();
        if (granted.Count == 0)
            throw new BusinessRuleViolationException(
                Errors.NoCouponTicketTypeSelected(_allowedTicketTypeIds.Select(id => id.Value).ToArray()));

        RedeemedAt = now;
        return granted;
    }

    /// <summary>
    /// Ends an unredeemed coupon early, e.g. a waitlist offer withdrawn because an admin registered its recipient for
    /// the ticket type. A redeemed coupon stays redeemed.
    /// </summary>
    public void Expire(DateTimeOffset now)
    {
        if (GetStatus(now) == CouponStatus.Active)
            ExpiresAt = now;
    }

    /// <summary>
    /// Rejects a selection holding any ticket type outside this coupon's allow-list. Used where the coupon is
    /// the only claim source for the whole selection (registering with a coupon), so it cannot be used to
    /// claim uncapped capacity for ticket types it was never issued for.
    /// </summary>
    public void EnsureAllowsAll(IReadOnlyList<TicketTypeId> ticketTypeIds)
    {
        var notAllowlisted = ticketTypeIds
            .Where(id => !_allowedTicketTypeIds.Contains(id))
            .Select(id => id.Value)
            .ToArray();
        if (notAllowlisted.Length > 0)
            throw new BusinessRuleViolationException(Errors.TicketTypeNotAllowlisted(notAllowlisted));
    }

    internal static class Errors
    {
        public static readonly Error NoTicketTypes = new(
            "coupon.no_ticket_types",
            "At least one ticket type must be specified.");

        public static Error UnknownTicketTypes(IReadOnlyList<Guid> ids) => new(
            "coupon.unknown_ticket_types",
            "One or more ticket types do not exist.",
            new Dictionary<string, object?> { ["ticketTypeIds"] = ids });

        public static readonly Error ExpiryMustBeInFuture = new(
            "coupon.expiry_must_be_in_future",
            "Expiry must be in the future.");

        public static readonly Error Expired = new(
            "coupon.expired",
            "This coupon has expired.",
            Type: ErrorType.Validation);

        public static readonly Error AlreadyRedeemed = new(
            "coupon.already_redeemed",
            "This coupon has already been used.",
            Type: ErrorType.Conflict);

        public static Error TicketTypeNotAllowlisted(Guid[] ids) => new(
            "coupon.ticket_type_not_allowed",
            "One or more ticket types are not allowed for this coupon.",
            Details: new Dictionary<string, object?> { ["ids"] = ids });

        public static Error NoCouponTicketTypeSelected(Guid[] allowedIds) => new(
            "coupon.no_coupon_ticket_type_selected",
            "The ticket selection must include at least one of this coupon's ticket types.",
            Type: ErrorType.Validation,
            Details: new Dictionary<string, object?> { ["allowedTicketTypeIds"] = allowedIds });

        public static readonly Error EmailMismatch = new(
            "coupon.email_mismatch",
            "The supplied email does not match the email this coupon was issued to.",
            Type: ErrorType.Validation);
    }
}

