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
    WaitlistOfferExpired = 4,

    /// <summary>
    /// The registration's last remaining selection — an active queue position or outstanding offer — was
    /// voluntarily given up by the attendee via a partner self-service update. Distinct from
    /// <see cref="TicketTypesRemoved"/> (organizer-driven) even though both are silent (no cancellation email),
    /// since the attendee initiated the change themselves.
    /// </summary>
    LeftWaitlist = 5
}
