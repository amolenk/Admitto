namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

public record SendRejectionEmailCommand(Guid AttendeeId, Guid RegistrationId) : ICommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
}
