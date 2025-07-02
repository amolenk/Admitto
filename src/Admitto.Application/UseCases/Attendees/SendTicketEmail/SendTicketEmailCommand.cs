namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

public record SendTicketEmailCommand(Guid RegistrationId) : ICommand
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
