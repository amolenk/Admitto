namespace Amolenk.Admitto.Application.UseCases.Registrations.RejectRegistration;

public record RejectRegistrationCommand(Guid RegistrationId) : Command;
