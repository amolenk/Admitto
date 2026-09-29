namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

/// <summary>
/// How a waitlist coupon came to be issued. Only <see cref="Automatic"/> coupons are backed by a freed slot.
/// </summary>
public enum WaitlistCouponOrigin
{
    /// <summary>Issued to the front of the queue because a slot was freed.</summary>
    Automatic = 0,

    /// <summary>Issued to a specific entry by an organizer (VIP promotion), not backed by a freed slot.</summary>
    Manual = 1,
}
