namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

public record ReserveTicketsCommand(Guid RegistrationId) : ICommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
}
