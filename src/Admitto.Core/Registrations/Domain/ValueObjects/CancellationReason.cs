namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

public enum CancellationReason
{
    AttendeeRequest = 0,
    VisaLetterDenied = 1,
    TicketTypesRemoved = 2,
    ReconfirmAutoCancel = 3,

    /// <summary>
    /// The registration's last remaining selection was a waitlist offer that lapsed unclaimed. Distinct from
    /// <see cref="TicketTypesRemoved"/> so the attendee gets the waitlist-offer-expired email instead of a
    /// second, redundant cancellation email.
    /// </summary>
    WaitlistOfferExpired = 4
}
