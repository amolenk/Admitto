namespace Amolenk.Admitto.Application.UseCases.Registrations.CompleteRegistration;

public record CompleteRegistrationCommand(Guid RegistrationId) : ICommand
{
    public Guid Id { get; init; } = Guid.NewGuid();
}