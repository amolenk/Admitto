using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Projections.Participation;

public class ParticipationHandler(IApplicationContext context)
    : IEventualDomainEventHandler<AttendeeRegisteredDomainEvent>,
        IEventualDomainEventHandler<AttendeeCanceledDomainEvent>,
        IEventualDomainEventHandler<AttendeeCanceledLateDomainEvent>,
        IEventualDomainEventHandler<SpeakerEngagementAddedDomainEvent>,
        IEventualDomainEventHandler<CrewAssignmentAddedDomainEvent>
{
    public async ValueTask HandleAsync(AttendeeRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpsertAttendeeRegistrationAsync(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.Email,
            domainEvent.RegistrationId,
            AttendeeStatus.Registered,
            domainEvent.RegistrationVersion,
            domainEvent.OccurredOn,
            cancellationToken);
    }
    
    public async ValueTask HandleAsync(AttendeeCanceledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpsertAttendeeRegistrationAsync(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.Email,
            domainEvent.RegistrationId,
            AttendeeStatus.Canceled,
            domainEvent.RegistrationVersion,
            domainEvent.OccurredOn,
            cancellationToken);
    }
    
    public async ValueTask HandleAsync(AttendeeCanceledLateDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpsertAttendeeRegistrationAsync(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.Email,
            domainEvent.RegistrationId,
            AttendeeStatus.CanceledLate,
            domainEvent.RegistrationVersion,
            domainEvent.OccurredOn,
            cancellationToken);
    }
    
    public async ValueTask HandleAsync(SpeakerEngagementAddedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpsertSpeakerEngagementAsync(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.Email,
            domainEvent.EngagementId,
            domainEvent.EngagementVersion,
            domainEvent.OccurredOn,
            cancellationToken);
    }

    public async ValueTask HandleAsync(CrewAssignmentAddedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpsertCrewAssignmentAsync(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.Email,
            domainEvent.AssignmentId,
            domainEvent.AssignmentVersion,
            domainEvent.OccurredOn,
            cancellationToken);
    }
    
    private async ValueTask UpsertAttendeeRegistrationAsync(
        Guid teamId,
        Guid ticketedEventId,
        string email,
        Guid registrationId,
        AttendeeStatus registrationStatus,
        uint registrationVersion,
        DateTimeOffset lastModifiedAt,
        CancellationToken cancellationToken)
    {
        var record = await GetOrCreateRecordAsync(teamId, ticketedEventId, email, cancellationToken);

        if (lastModifiedAt > record.LastModifiedAt || registrationVersion > record.AttendeeRegistrationVersion)
        {
            record.AttendeeRegistrationId = registrationId;
            record.AttendeeRegistrationStatus = registrationStatus;
            record.AttendeeRegistrationVersion = registrationVersion;
            record.LastModifiedAt = lastModifiedAt;
        }
    }

    private async ValueTask UpsertSpeakerEngagementAsync(
        Guid teamId,
        Guid ticketedEventId,
        string email,
        Guid engagementId,
        uint engagementVersion,
        DateTimeOffset lastModifiedAt,
        CancellationToken cancellationToken)
    {
        var record = await GetOrCreateRecordAsync(teamId, ticketedEventId, email, cancellationToken);

        if (lastModifiedAt > record.LastModifiedAt || engagementVersion > record.SpeakerEngagementVersion)
        {
            record.SpeakerEngagementId = engagementId;
            record.SpeakerEngagementVersion = engagementVersion;
            record.LastModifiedAt = lastModifiedAt;
        }
    }
    
    private async ValueTask UpsertCrewAssignmentAsync(
        Guid teamId,
        Guid ticketedEventId,
        string email,
        Guid assignmentId,
        uint assignmentVersion,
        DateTimeOffset lastModifiedAt,
        CancellationToken cancellationToken)
    {
        var record = await GetOrCreateRecordAsync(teamId, ticketedEventId, email, cancellationToken);

        if (lastModifiedAt > record.LastModifiedAt || assignmentVersion > record.CrewAssignmentVersion)
        {
            record.CrewAssignmentId = assignmentId;
            record.CrewAssignmentVersion = assignmentVersion;
            record.LastModifiedAt = lastModifiedAt;
        }
    }
    
    private async ValueTask<ParticipationView> GetOrCreateRecordAsync(
        Guid teamId,
        Guid ticketedEventId,
        string email,
        CancellationToken cancellationToken)
    {
        var record = await context.ParticipationView.FindAsync(
            [teamId, ticketedEventId, email],
            cancellationToken);
            
        if (record is null)
        {
            record = new ParticipationView
            {
                TeamId = teamId,
                TicketedEventId = ticketedEventId,
                Email = email
            };

            context.ParticipationView.Add(record);
        };

        return record;
    }
}