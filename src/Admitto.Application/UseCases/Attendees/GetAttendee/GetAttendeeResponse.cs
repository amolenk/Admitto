using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.GetAttendee;

public record GetAttendeeResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    RegistrationStatus RegistrationStatus,
    string Signature);
