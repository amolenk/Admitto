using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.PendingRegistrations.GetPendingRegistrations;

public record GetPendingRegistrationsResponse(PendingRegistrationDto[] RegistrationRequests);

public record PendingRegistrationDto(
    Guid RegistrationId,
    string Email,
    string FirstName,
    string LastName,
    RegistrationRequestStatus Status,
    DateTime LastChangedAt);
