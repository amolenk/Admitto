namespace Amolenk.Admitto.Domain.ValueObjects;

public enum AttendeeStatus
{
    Registered = 0,
    Reconfirmed = 1,
    AttendedEvent = 2,
    CanceledOnTime = 3,
    CanceledLastMinute = 4,
    SkippedEvent = 5
}