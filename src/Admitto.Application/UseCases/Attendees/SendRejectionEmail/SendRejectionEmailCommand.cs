namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

public record SendRejectionEmailCommand(Guid RegistrationId) : ICommand
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
