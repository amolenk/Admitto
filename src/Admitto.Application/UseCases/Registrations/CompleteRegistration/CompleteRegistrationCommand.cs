namespace Amolenk.Admitto.Application.UseCases.Registrations.CompleteRegistration;

public record CompleteRegistrationCommand(Guid RegistrationId) : Command
{
    public Guid Id { get; init; } = Guid.NewGuid();
}