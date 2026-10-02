using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

public class WaitlistCoupon : Entity<CouponId>
{
    // Required for EF Core
    // ReSharper disable once UnusedMember.Local
    private WaitlistCoupon()
    {
    }

    internal WaitlistCoupon(
        CouponId id,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        WaitlistCouponOrigin origin)
        : base(id)
    {
        Status = WaitlistCouponStatus.Issued;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        Origin = origin;
    }

    public WaitlistCouponStatus Status { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }

    /// <summary>
    /// When the offer lapses; copied from the <see cref="Coupon"/> at issuance.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    public WaitlistCouponOrigin Origin { get; private set; }

    internal void Redeem()
    {
        if (Status != WaitlistCouponStatus.Issued)
            throw new BusinessRuleViolationException(Errors.CouponNotRedeemable);

        Status = WaitlistCouponStatus.Redeemed;
    }

    internal void Expire()
    {
        if (Status != WaitlistCouponStatus.Issued)
            throw new BusinessRuleViolationException(Errors.CouponNotExpirable);

        Status = WaitlistCouponStatus.Expired;
    }

    internal static class Errors
    {
        public static readonly Error CouponNotRedeemable = new(
            "waitlist.coupon_not_redeemable",
            "The waitlist coupon cannot be redeemed because it has already been redeemed or has expired.",
            Type: ErrorType.Conflict);

        public static readonly Error CouponNotExpirable = new(
            "waitlist.coupon_not_expirable",
            "The waitlist coupon cannot be expired because it has already been redeemed or has expired.",
            Type: ErrorType.Conflict);
    }
}
