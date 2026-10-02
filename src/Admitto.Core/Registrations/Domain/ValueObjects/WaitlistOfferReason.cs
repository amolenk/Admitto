namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

/// <summary>
/// Why a waitlist coupon was issued, independent of <see cref="WaitlistCouponOrigin"/> (which decides capacity
/// accounting). Drives the wording of the waitlist offer email: only <see cref="AutomaticPromotion"/> is actually
/// "you've reached the top of the waitlist" — a VIP promotion skips the queue, and a capacity-opened release
/// offers everyone at once regardless of position.
/// </summary>
public enum WaitlistOfferReason
{
    /// <summary>A seat became available and the offer went to the front of the queue, in order.</summary>
    AutomaticPromotion = 0,

    /// <summary>An organizer promoted this entry out of order (VIP promotion).</summary>
    VipPromotion = 1,

    /// <summary>The ticket type's capacity limit was removed, so every active entry got an offer at once.</summary>
    CapacityOpenedForEveryone = 2,
}
