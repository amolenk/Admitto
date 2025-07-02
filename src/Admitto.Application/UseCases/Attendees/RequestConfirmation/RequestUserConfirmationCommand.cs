namespace Amolenk.Admitto.Application.UseCases.Attendees.RequestConfirmation;

public record RequestUserConfirmationCommand(Guid TicketedEventId, Guid RegistrationId) : ICommand
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
