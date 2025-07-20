using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.PendingRegistrations.GetPendingRegistration;

public record GetPendingRegistrationResponse(string Email, RegistrationRequestStatus Status);
