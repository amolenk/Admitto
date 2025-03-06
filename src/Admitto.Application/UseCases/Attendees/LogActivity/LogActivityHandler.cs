using Amolenk.Admitto.Application.Common.ReadModels;

namespace Amolenk.Admitto.Application.UseCases.Attendees.LogActivity;

/// <summary>
/// Logs an activity for an attendee.
/// </summary>
public class LogActivityHandler(IApplicationContext context, ILogger<LogActivityHandler> logger) 
    : ICommandHandler<LogActivityCommand>
{
    public async ValueTask HandleAsync(LogActivityCommand command, CancellationToken cancellationToken)
    {
        // TODO Guard against duplicate activities
        await context.AttendeeActivities.AddAsync(new AttendeeActivityReadModel(command.CommandId, command.AttendeeId,
            command.Activity, command.Timestamp), cancellationToken);
    
        // TODO Improve
        logger.LogInformation("Activity: {Activity}", command.Activity);
    }
}
