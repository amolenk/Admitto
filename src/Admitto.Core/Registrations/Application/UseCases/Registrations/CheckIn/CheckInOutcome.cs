namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CheckIn;

public enum CheckInOutcome
{
    Success,
    AlreadyCheckedIn,
    Cancelled,
    Waitlisted,
    InvalidForEvent,
    EventNotActive
}
