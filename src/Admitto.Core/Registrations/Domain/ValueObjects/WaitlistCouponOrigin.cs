namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

/// <summary>
/// How a waitlist coupon came to be issued. For history and display only: every offer, whatever its origin, holds a
/// seat on its ticket type (<see cref="Entities.TicketType.WaitlistHeldCapacity"/>) until it is redeemed or expires.
/// </summary>
public enum WaitlistCouponOrigin
{
    /// <summary>Issued to the front of the queue because a seat became available.</summary>
    Automatic = 0,

    /// <summary>Issued to a specific entry by an organizer (VIP promotion), whether or not a seat was available.</summary>
    Manual = 1,
}
