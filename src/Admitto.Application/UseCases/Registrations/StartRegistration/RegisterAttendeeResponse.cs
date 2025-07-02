using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Registrations.StartRegistration;

public record RegisterAttendeeResponse(Guid Id)
{
    public static RegisterAttendeeResponse FromAttendeeRegistration(AttendeeRegistration attendeeRegistration)
    {
        return new RegisterAttendeeResponse(attendeeRegistration.Id);
    }
}