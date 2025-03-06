namespace Amolenk.Admitto.Application.UseCases.Attendees.LogActivity;

public record LogActivityCommand(Guid AttendeeId, string Activity, DateTime Timestamp) : ICommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
}
