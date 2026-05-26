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

    internal WaitlistCoupon(CouponId id, DateTimeOffset issuedAt)
        : base(id)
    {
        Status = WaitlistCouponStatus.Issued;
        IssuedAt = issuedAt;
    }

    public WaitlistCouponStatus Status { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }

    internal void Redeem()
    {
        if (Status != WaitlistCouponStatus.Issued)
            throw new BusinessRuleViolationException(Errors.CouponNotRedeemable);

        Status = WaitlistCouponStatus.Redeemed;
    }

    internal void Revoke()
    {
        if (Status != WaitlistCouponStatus.Issued)
            throw new BusinessRuleViolationException(Errors.CouponNotRevokable);

        Status = WaitlistCouponStatus.Revoked;
    }

    internal static class Errors
    {
        public static readonly Error CouponNotRedeemable = new(
            "waitlist.coupon_not_redeemable",
            "The waitlist coupon cannot be redeemed because it has already been redeemed or revoked.",
            Type: ErrorType.Conflict);

        public static readonly Error CouponNotRevokable = new(
            "waitlist.coupon_not_revokable",
            "The waitlist coupon cannot be revoked because it has already been redeemed or revoked.",
            Type: ErrorType.Conflict);
    }
}
