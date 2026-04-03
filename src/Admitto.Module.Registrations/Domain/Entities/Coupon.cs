using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.Entities;

/// <summary>
/// Represents a single-use invitation to register for an event with specific ticket types.
/// Coupons bypass capacity and email domain restrictions. They optionally bypass the registration window.
/// </summary>
public class Coupon : Aggregate<CouponId>
{
    private readonly List<string> _allowedTicketTypeSlugs = [];

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
        IReadOnlyList<string> allowedTicketTypeSlugs,
        DateTimeOffset expiresAt,
        bool bypassRegistrationWindow)
        : base(id)
    {
        EventId = eventId;
        Code = code;
        Email = email;
        ExpiresAt = expiresAt;
        BypassRegistrationWindow = bypassRegistrationWindow;

        _allowedTicketTypeSlugs = allowedTicketTypeSlugs.ToList();
    }

    public TicketedEventId EventId { get; private set; }
    public CouponCode Code { get; private set; }
    public EmailAddress Email { get; private set; }
    public IReadOnlyList<string> AllowedTicketTypeSlugs => _allowedTicketTypeSlugs.AsReadOnly();
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
        IReadOnlyList<string> requestedTicketTypeSlugs,
        DateTimeOffset expiresAt,
        bool bypassRegistrationWindow,
        IReadOnlyList<TicketTypeInfo> availableTicketTypes,
        DateTimeOffset now)
    {
        // Validate at least one ticket type.
        if (requestedTicketTypeSlugs.Count == 0)
        {
            throw new BusinessRuleViolationException(Errors.NoTicketTypes);
        }

        // Validate all requested ticket types exist and are not cancelled.
        var availableLookup = availableTicketTypes.ToDictionary(t => t.Slug);
        var unknownSlugs = new List<string>();
        var cancelledSlugs = new List<string>();

        foreach (var slug in requestedTicketTypeSlugs)
        {
            if (!availableLookup.TryGetValue(slug, out var ticketType))
            {
                unknownSlugs.Add(slug);
            }
            else if (ticketType.IsCancelled)
            {
                cancelledSlugs.Add(slug);
            }
        }

        if (unknownSlugs.Count > 0)
        {
            throw new BusinessRuleViolationException(Errors.UnknownTicketTypes(unknownSlugs));
        }

        if (cancelledSlugs.Count > 0)
        {
            throw new BusinessRuleViolationException(Errors.CancelledTicketTypes(cancelledSlugs));
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
            requestedTicketTypeSlugs,
            expiresAt,
            bypassRegistrationWindow);

        coupon.AddDomainEvent(new CouponCreatedDomainEvent(
            coupon.Id,
            coupon.EventId,
            coupon.Email));

        return coupon;
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

        public static Error UnknownTicketTypes(IReadOnlyList<string> slugs) => new(
            "coupon.unknown_ticket_types",
            "One or more ticket types do not exist.",
            new Dictionary<string, object?> { ["ticketTypeSlugs"] = slugs });

        public static Error CancelledTicketTypes(IReadOnlyList<string> slugs) => new(
            "coupon.cancelled_ticket_types",
            "One or more ticket types are cancelled.",
            new Dictionary<string, object?> { ["ticketTypeSlugs"] = slugs });

        public static readonly Error ExpiryMustBeInFuture = new(
            "coupon.expiry_must_be_in_future",
            "Expiry must be in the future.");

        public static readonly Error CouponAlreadyRedeemed = new(
            "coupon.already_redeemed",
            "Cannot revoke a coupon that has already been redeemed.",
            Type: ErrorType.Conflict);
    }
}

