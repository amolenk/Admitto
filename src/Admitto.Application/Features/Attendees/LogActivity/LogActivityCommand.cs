namespace Amolenk.Admitto.Application.Features.Attendees.LogActivity;

public record LogActivityCommand(Guid AttendeeId, string Activity, DateTime Timestamp) : ICommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
}
