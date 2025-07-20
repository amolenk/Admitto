using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Registrations.StartRegistration;

public record StartRegistrationRequest(RegistrationType Type, string Email, string FirstName, string LastName,
    Dictionary<string, int> Tickets, Dictionary<string, string>? AdditionalDetails = null);
