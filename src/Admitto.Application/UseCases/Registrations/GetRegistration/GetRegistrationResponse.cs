using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Registrations.GetRegistration;

public record GetRegistrationResponse(string Email, RegistrationStatus Status, string Signature);
