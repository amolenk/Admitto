namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

/// <summary>
/// How a waitlist coupon came to be issued. An automatic offer holds a public seat on its ticket type
/// (<see cref="Entities.TicketType.WaitlistHeldCapacity"/>) until it is redeemed or expires, and redeems into a public
/// ticket. A manual (VIP) offer takes no hold and redeems into an admin ticket on top of public capacity.
/// </summary>
public enum WaitlistCouponOrigin
{
    /// <summary>Issued to the front of the queue because a seat became available.</summary>
    Automatic = 0,

    /// <summary>Issued to a specific entry by an organizer (VIP promotion), on top of public capacity.</summary>
    Manual = 1,
}
