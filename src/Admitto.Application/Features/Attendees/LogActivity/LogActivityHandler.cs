namespace Amolenk.Admitto.Application.Features.Attendees.LogActivity;

/// <summary>
/// Log the activity for an attendee.
/// </summary>
public class LogActivityHandler : IRequestHandler<LogActivityCommand>
{
    public Task Handle(LogActivityCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine(request.Activity);
        return Task.CompletedTask;
    }
}
