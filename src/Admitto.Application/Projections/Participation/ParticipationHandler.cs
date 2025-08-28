using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Projections.Participation;

public class ParticipationHandler(IApplicationContext context)
    : IEventualDomainEventHandler<AttendeeRegisteredDomainEvent>,
        IEventualDomainEventHandler<AttendeeCanceledDomainEvent>,
        IEventualDomainEventHandler<AttendeeCanceledLateDomainEvent>,
        IEventualDomainEventHandler<ContributorRegisteredDomainEvent>
{
    public async ValueTask HandleAsync(AttendeeRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpsertAttendeeRegistrationAsync(
            domainEvent.TicketedEventId,
            domainEvent.RegistrationId,
            domainEvent.Email,
            AttendeeStatus.Registered,
            domainEvent.OccurredOn,
            cancellationToken);
    }

    public async ValueTask HandleAsync(AttendeeCanceledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpsertAttendeeRegistrationAsync(
            domainEvent.TicketedEventId,
            domainEvent.RegistrationId,
            domainEvent.Email,
            AttendeeStatus.Canceled,
            domainEvent.OccurredOn,
            cancellationToken);
    }

    public async ValueTask HandleAsync(AttendeeCanceledLateDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await UpsertAttendeeRegistrationAsync(
            domainEvent.TicketedEventId,
            domainEvent.RegistrationId,
            domainEvent.Email,
            AttendeeStatus.CanceledLate,
            domainEvent.OccurredOn,
            cancellationToken);
    }

    public async ValueTask HandleAsync(
        ContributorRegisteredDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await UpsertContributorRegistrationAsync(
            domainEvent.TicketedEventId,
            domainEvent.RegistrationId,
            domainEvent.Email,
            domainEvent.Role,
            domainEvent.OccurredOn,
            cancellationToken);
    }

    private async ValueTask UpsertAttendeeRegistrationAsync(
        Guid ticketedEventId,
        Guid registrationId,
        string email,
        AttendeeStatus attendeeStatus,
        DateTimeOffset lastModifiedAt,
        CancellationToken cancellationToken)
    {
        var record = await GetOrCreateRecordAsync(
            ticketedEventId,
            registrationId,
            email,
            cancellationToken);

        if (lastModifiedAt > record.LastModifiedAt)
        {
            record.AttendeeStatus = attendeeStatus;
            record.LastModifiedAt = lastModifiedAt;
        }
    }

    private async ValueTask UpsertContributorRegistrationAsync(
        Guid ticketedEventId,
        Guid registrationId,
        string email,
        ContributorRole role,
        DateTimeOffset lastModifiedAt,
        CancellationToken cancellationToken)
    {
        var record = await GetOrCreateRecordAsync(
            ticketedEventId,
            registrationId,
            email,
            cancellationToken);

        if (lastModifiedAt > record.LastModifiedAt)
        {
            record.ContributorRole = role;
            record.LastModifiedAt = lastModifiedAt;
        }
    }

    private async ValueTask<ParticipationView> GetOrCreateRecordAsync(
        Guid ticketedEventId,
        Guid registrationId,
        string email,
        CancellationToken cancellationToken)
    {
        var record = await context.ParticipationView.FindAsync(
            [ticketedEventId, email],
            cancellationToken);

        if (record is null)
        {
            record = new ParticipationView
            {
                TicketedEventId = ticketedEventId,
                RegistrationId = registrationId,
                Email = email
            };

            context.ParticipationView.Add(record);
        }

        ;

        return record;
    }
}