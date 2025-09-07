using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.Attendees.RecordAttendance;

/// <summary>
/// Records the attendance of a participant at a ticketed event.
/// </summary>
public class RecordAttendanceHandler(IApplicationContext context)
    : ICommandHandler<RecordAttendanceCommand>
{
    public async ValueTask HandleAsync(RecordAttendanceCommand command, CancellationToken cancellationToken)
    {
        var attendee = await context.Attendees.FindAsync([command.AttendeeId], cancellationToken);
        if (attendee is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }

        if (command.Attended)
        {
            attendee.MarkAsCheckedIn();
        }
        else
        {
            attendee.MarkAsNoShow();
        }
    }
}