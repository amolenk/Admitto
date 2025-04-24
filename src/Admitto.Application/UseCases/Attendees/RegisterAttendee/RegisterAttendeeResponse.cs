using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

public record RegisterAttendeeResponse(Guid Id)
{
    public static RegisterAttendeeResponse FromAttendeeRegistration(AttendeeRegistration attendeeRegistration)
    {
        return new RegisterAttendeeResponse(attendeeRegistration.Id);
    }
}