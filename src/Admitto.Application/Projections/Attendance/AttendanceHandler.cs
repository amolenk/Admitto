using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Projections.Attendance;

public class AttendanceHandler(IApplicationContext context)
    : IEventualDomainEventHandler<AttendeeCheckedInDomainEvent>,
        IEventualDomainEventHandler<AttendeeCanceledDomainEvent>,
        IEventualDomainEventHandler<AttendeeNoShowDomainEvent>
{
    public async ValueTask HandleAsync(AttendeeCheckedInDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpdateAttendanceRecordAsync(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.AttendeeId,
            AttendanceType.CheckedIn,
            domainEvent.AttendeeVersion,
            cancellationToken);
    }

    public async ValueTask HandleAsync(AttendeeCanceledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpdateAttendanceRecordAsync(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.AttendeeId,
            domainEvent.LateCancellation ? AttendanceType.CanceledLate : AttendanceType.Canceled,
            domainEvent.AttendeeVersion,
            cancellationToken);
    }

    public async ValueTask HandleAsync(AttendeeNoShowDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpdateAttendanceRecordAsync(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.AttendeeId,
            AttendanceType.NoShow,
            domainEvent.AttendeeVersion,
            cancellationToken);
    }

    private async ValueTask UpdateAttendanceRecordAsync(
        Guid teamId,
        Guid ticketedEventId,
        Guid attendeeId,
        AttendanceType attendanceType,
        uint attendeeVersion,
        CancellationToken cancellationToken)
    {
        var record = await context.AttendanceView.FindAsync(
            [teamId, ticketedEventId, attendeeId],
            cancellationToken);

        if (record is null)
        {
            record = new AttendanceView
            {
                TeamId = teamId,
                TicketedEventId = ticketedEventId,
                AttendeeId = attendeeId,
                AttendanceType = attendanceType,
                AttendeeVersion = attendeeVersion
            };
        }
        else if (attendeeVersion > record.AttendeeVersion)
        {
            record.AttendanceType = attendanceType;
        }
        else
        {
            // TODO Log a warning (or information)
        }
    }
}