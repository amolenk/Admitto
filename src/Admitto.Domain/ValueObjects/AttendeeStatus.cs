namespace Amolenk.Admitto.Domain.ValueObjects;

public enum AttendeeStatus
{
    PendingVerification = 0,
    VerificationFailed = 1,
    PendingTickets = 2, // Invited attendees go directly to this status
    Registered = 3,
    RegistrationFailed = 4,
    Canceled = 5,
}