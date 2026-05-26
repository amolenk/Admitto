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
        TeamId teamId,
        CouponCode code,
        EmailAddress email,
        IReadOnlyList<TicketTypeId> allowedTicketTypeIds,
        DateTimeOffset expiresAt,
        bool bypassRegistrationWindow,
        CouponSource source)
        : base(id)
    {
        EventId = eventId;
        TeamId = teamId;
        Code = code;
        Email = email;
        ExpiresAt = expiresAt;
        BypassRegistrationWindow = bypassRegistrationWindow;
        Source = source;

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
        TeamId teamId,
        EmailAddress email,
        IReadOnlyList<TicketTypeId> requestedTicketTypeIds,
        DateTimeOffset expiresAt,
        bool bypassRegistrationWindow,
        IReadOnlyList<TicketTypeInfo> availableTicketTypes,
        DateTimeOffset now,
        CouponSource source = CouponSource.Organiser)
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
            source);

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

    public void Redeem(
        EmailAddress email,
        IReadOnlyList<TicketTypeId> ticketTypeIds,
        DateTimeOffset now)
    {
        var status = GetStatus(now);
        if (status == CouponStatus.Expired)
            throw new BusinessRuleViolationException(Errors.Expired);
        if (status == CouponStatus.Redeemed)
            throw new BusinessRuleViolationException(Errors.AlreadyRedeemed);
        if (status == CouponStatus.Revoked)
            throw new BusinessRuleViolationException(Errors.Revoked);

        var notAllowlisted = ticketTypeIds
            .Where(id => !_allowedTicketTypeIds.Any(allowed => allowed == id))
            .Select(id => id.Value)
            .ToArray();
        if (notAllowlisted.Length > 0)
            throw new BusinessRuleViolationException(Errors.TicketTypeNotAllowlisted(notAllowlisted));

        if (Email != email)
            throw new BusinessRuleViolationException(Errors.EmailMismatch);

        RedeemedAt = now;
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

        public static readonly Error ExpiryMustBeInFuture = new(
            "coupon.expiry_must_be_in_future",
            "Expiry must be in the future.");

        public static readonly Error CouponAlreadyRedeemed = new(
            "coupon.already_redeemed",
            "Cannot revoke a coupon that has already been redeemed.",
            Type: ErrorType.Conflict);

        public static readonly Error Expired = new(
            "coupon.expired",
            "This coupon has expired.",
            Type: ErrorType.Validation);

        public static readonly Error AlreadyRedeemed = new(
            "coupon.already_redeemed",
            "This coupon has already been used.",
            Type: ErrorType.Conflict);

        public static readonly Error Revoked = new(
            "coupon.revoked",
            "This coupon has been revoked.",
            Type: ErrorType.Conflict);

        public static Error TicketTypeNotAllowlisted(Guid[] ids) => new(
            "coupon.ticket_type_not_allowed",
            "One or more ticket types are not allowed for this coupon.",
            Details: new Dictionary<string, object?> { ["ids"] = ids });

        public static readonly Error EmailMismatch = new(
            "coupon.email_mismatch",
            "The supplied email does not match the email this coupon was issued to.",
            Type: ErrorType.Validation);
    }
}

