namespace Amolenk.Admitto.Domain.ValueObjects;

public enum AttendeeStatus
{
    Unverified = 0,
    Verified = 1,
    VerificationFailed = 2,
    Registered = 3,
    Rejected = 4,
    Reconfirmed = 5,
    AttendedEvent = 6,
    CanceledOnTime = 7,
    CanceledLastMinute = 8,
    SkippedEvent = 9
}