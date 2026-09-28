namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

/// <summary>
/// Determines which capacity pool a ticket claim consumes, and whether public
/// enforcement (self-service availability, sold-out/waitlist gates) applies.
/// </summary>
public enum ClaimMode
{
    /// <summary>
    /// Self-service registration. Enforced against public availability
    /// (throws when sold out or in WaitlistMode) and counts as public usage.
    /// </summary>
    Public,

    /// <summary>
    /// Waitlist coupon redemption. Not enforced — the coupon already reserved
    /// the slot when it was issued — but counts as public usage, not reserved usage.
    /// </summary>
    PublicUncapped,

    /// <summary>
    /// Admin registration or general (non-waitlist) coupon redemption. Uncapped by
    /// design: consumes the reserved buffer first, then spills into the public pool
    /// once the buffer is exhausted.
    /// </summary>
    Reserved
}
