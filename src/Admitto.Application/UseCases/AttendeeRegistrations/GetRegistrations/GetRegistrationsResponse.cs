using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.AttendeeRegistrations.GetRegistrations;

public record GetRegistrationsResponse(RegistrationDto[] Registrations);

public record RegistrationDto(
    Guid RegistrationId,
    string Email,
    string FirstName,
    string LastName,
    RegistrationStatus Status,
    DateTimeOffset LastChangedAt);
