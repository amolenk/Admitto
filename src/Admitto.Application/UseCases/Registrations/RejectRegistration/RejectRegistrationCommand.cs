namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

public class RejectRegistrationCommand : ICommand
{
    public Guid Id { get; init; } = Guid.NewGuid();
}