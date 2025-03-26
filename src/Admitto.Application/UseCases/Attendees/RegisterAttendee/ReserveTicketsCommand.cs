namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

public record ReserveTicketsCommand(Guid RegistrationId) : ICommand
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
