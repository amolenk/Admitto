using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Application.Features.Attendees.LogActivity;

/// <summary>
/// Log the activity for an attendee.
/// </summary>
public class LogActivityHandler(ILogger<LogActivityHandler> logger) : ICommandHandler<LogActivityCommand>
{
    public ValueTask HandleAsync(LogActivityCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Activity: {Activity}", command.Activity);
        return ValueTask.CompletedTask;
    }
}
