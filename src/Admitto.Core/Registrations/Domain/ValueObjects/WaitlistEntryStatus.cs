namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

public enum WaitlistEntryStatus
{
    Active = 0,
    Removed = 1,

    /// <summary>
    /// Holds an outstanding, unredeemed waitlist offer for this entry's email. Not counted in the queue
    /// (position/count), but still a current selection on the registration: it is not <see cref="Removed"/>
    /// until the offer is redeemed, withdrawn, or expires.
    /// </summary>
    Offered = 2,
}
