namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

/// <summary>
/// The capacity pool a ticket claim belongs to (see ADR-019). Recorded on the ticket so that a release credits the
/// same pool back.
/// </summary>
public enum ClaimMode
{
    /// <summary>
    /// Counts against <c>PublicCapacity</c>. Self-service claims are enforced (rejected when sold out or in
    /// WaitlistMode); redeeming an automatic waitlist offer is not, because it converts the offer's hold.
    /// </summary>
    Public,

    /// <summary>
    /// Admin registration, a ticket added by an admin edit, an organiser coupon or a VIP waitlist offer. Never
    /// enforced: comes on top of <c>PublicCapacity</c> and never uses or frees a public seat.
    /// </summary>
    Admin
}
