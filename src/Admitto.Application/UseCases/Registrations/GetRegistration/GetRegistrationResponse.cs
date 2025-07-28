using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Registrations.GetRegistrations;

public record GetRegistrationResponse(string Email, RegistrationStatus Status);
