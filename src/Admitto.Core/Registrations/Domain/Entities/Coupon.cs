using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

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
        CouponCode code,
        EmailAddress email,
        IReadOnlyList<TicketTypeId> allowedTicketTypeIds,
        DateTimeOffset expiresAt,
        bool bypassRegistrationWindow)
        : base(id)
    {
        EventId = eventId;
        Code = code;
        Email = email;
        ExpiresAt = expiresAt;
        BypassRegistrationWindow = bypassRegistrationWindow;

        _allowedTicketTypeIds = allowedTicketTypeIds.ToList();
    }

    public TicketedEventId EventId { get; private set; }
    public CouponCode Code { get; private set; }
    public EmailAddress Email { get; private set; }
    public IReadOnlyList<TicketTypeId> AllowedTicketTypeIds => _allowedTicketTypeIds.AsReadOnly();
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool BypassRegistrationWindow { get; private set; }
    public DateTimeOffset? RedeemedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public CouponStatus GetStatus(DateTimeOffset now)
    {
        if (RedeemedAt.HasValue) return CouponStatus.Redeemed;
        if (RevokedAt.HasValue) return CouponStatus.Revoked;
        if (ExpiresAt < now) return CouponStatus.Expired;
        return CouponStatus.Active;
    }

    public static Coupon Create(
        TicketedEventId eventId,
        EmailAddress email,
        IReadOnlyList<TicketTypeId> requestedTicketTypeIds,
        DateTimeOffset expiresAt,
        bool bypassRegistrationWindow,
        IReadOnlyList<TicketTypeInfo> availableTicketTypes,
        DateTimeOffset now)
    {
        // Validate at least one ticket type.
        if (requestedTicketTypeIds.Count == 0)
        {
            throw new BusinessRuleViolationException(Errors.NoTicketTypes);
        }

        // Validate all requested ticket types exist and are not cancelled.
        var availableLookup = availableTicketTypes.ToDictionary(t => t.Id);
        var unknownIds = new List<Guid>();
        var cancelledIds = new List<Guid>();

        foreach (var id in requestedTicketTypeIds)
        {
            if (!availableLookup.TryGetValue(id, out var ticketType))
            {
                unknownIds.Add(id.Value);
            }
            else if (ticketType.IsCancelled)
            {
                cancelledIds.Add(id.Value);
            }
        }

        if (unknownIds.Count > 0)
        {
            throw new BusinessRuleViolationException(Errors.UnknownTicketTypes(unknownIds));
        }

        if (cancelledIds.Count > 0)
        {
            throw new BusinessRuleViolationException(Errors.CancelledTicketTypes(cancelledIds));
        }

        // Validate expiry is in the future.
        if (expiresAt <= now)
        {
            throw new BusinessRuleViolationException(Errors.ExpiryMustBeInFuture);
        }

        var coupon = new Coupon(
            CouponId.New(),
            eventId,
            CouponCode.New(),
            email,
            requestedTicketTypeIds,
            expiresAt,
            bypassRegistrationWindow);

        coupon.AddDomainEvent(new CouponCreatedDomainEvent(
            coupon.Id,
            coupon.EventId,
            coupon.Email));

        return coupon;
    }

    public void Redeem()
    {
        if (RevokedAt.HasValue)
            throw new BusinessRuleViolationException(Errors.CouponAlreadyRevoked);

        if (RedeemedAt.HasValue)
            throw new BusinessRuleViolationException(Errors.CouponAlreadyRedeemed);

        RedeemedAt = DateTimeOffset.UtcNow;
    }

    public void Revoke()
    {
        if (RedeemedAt.HasValue)
        {
            throw new BusinessRuleViolationException(Errors.CouponAlreadyRedeemed);
        }

        // Revoking an already-revoked or expired coupon is idempotent.
        RevokedAt ??= DateTimeOffset.UtcNow;
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

        public static Error CancelledTicketTypes(IReadOnlyList<Guid> ids) => new(
            "coupon.cancelled_ticket_types",
            "One or more ticket types are cancelled.",
            new Dictionary<string, object?> { ["ticketTypeIds"] = ids });

        public static readonly Error ExpiryMustBeInFuture = new(
            "coupon.expiry_must_be_in_future",
            "Expiry must be in the future.");

        public static readonly Error CouponAlreadyRedeemed = new(
            "coupon.already_redeemed",
            "Cannot revoke a coupon that has already been redeemed.",
            Type: ErrorType.Conflict);

        public static readonly Error CouponAlreadyRevoked = new(
            "coupon.already_revoked",
            "This coupon has been revoked.",
            Type: ErrorType.Conflict);
    }
}

