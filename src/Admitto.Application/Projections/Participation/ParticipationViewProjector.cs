using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Projections.Participation;

public class ParticipationViewProjector(IApplicationContext context)
    : IEventualDomainEventHandler<AttendeeRegisteredDomainEvent>,
        IEventualDomainEventHandler<AttendeeCanceledDomainEvent>,
        IEventualDomainEventHandler<AttendeeCanceledLateDomainEvent>,
        IEventualDomainEventHandler<ContributorAddedDomainEvent>,
        IEventualDomainEventHandler<ContributorRemovedDomainEvent>
{
    public async ValueTask HandleAsync(AttendeeRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var record = await GetOrCreateRecordAsync(
            domainEvent.ParticipantId,
            domainEvent.TicketedEventId,
            cancellationToken);

        if (domainEvent.OccurredOn > record.LastModifiedAt)
        {
            record.Email = domainEvent.Email;
            record.FirstName = domainEvent.FirstName;
            record.LastName = domainEvent.LastName;
            record.AttendeeStatus = ParticipationAttendeeStatus.Registered;
            record.AttendeeId = domainEvent.AttendeeId;
            record.LastModifiedAt = domainEvent.OccurredOn;
        }
    }

    public async ValueTask HandleAsync(AttendeeCanceledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var record = await GetOrCreateRecordAsync(
            domainEvent.ParticipantId,
            domainEvent.TicketedEventId,
            cancellationToken);

        if (domainEvent.OccurredOn > record.LastModifiedAt)
        {
            record.AttendeeStatus = ParticipationAttendeeStatus.Canceled;
            record.AttendeeId = null;
            record.LastModifiedAt = domainEvent.OccurredOn;
        }
    }

    public async ValueTask HandleAsync(AttendeeCanceledLateDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var record = await GetOrCreateRecordAsync(
            domainEvent.ParticipantId,
            domainEvent.TicketedEventId,
            cancellationToken);

        if (domainEvent.OccurredOn > record.LastModifiedAt)
        {
            record.AttendeeStatus = ParticipationAttendeeStatus.Canceled;
            record.AttendeeId = null;
            record.LastModifiedAt = domainEvent.OccurredOn;
        }
    }
    
    public async ValueTask HandleAsync(ContributorAddedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var record = await GetOrCreateRecordAsync(
            domainEvent.ParticipantId,
            domainEvent.TicketedEventId,
            cancellationToken);

        if (domainEvent.OccurredOn > record.LastModifiedAt)
        {
            // If the personal details are not set yet, set them now.
            if (string.IsNullOrWhiteSpace(record.Email))
            {
                record.Email = domainEvent.Email;
                record.FirstName = domainEvent.FirstName;
                record.LastName = domainEvent.LastName;
            }

            record.ContributorStatus = ParticipationContributorStatus.Active;
            record.ContributorId = domainEvent.ContributorId;
            record.LastModifiedAt = domainEvent.OccurredOn;
        }
    }

    public async ValueTask HandleAsync(ContributorRemovedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var record = await GetOrCreateRecordAsync(
            domainEvent.ParticipantId,
            domainEvent.TicketedEventId,
            cancellationToken);

        if (domainEvent.OccurredOn > record.LastModifiedAt)
        {
            record.ContributorStatus = ParticipationContributorStatus.Removed;
            record.ContributorId = null;
            record.LastModifiedAt = domainEvent.OccurredOn;
        }
    }

    private async ValueTask<ParticipationView> GetOrCreateRecordAsync(
        Guid participantId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var record = await context.ParticipationView.FindAsync(
            [participantId],
            cancellationToken);

        if (record is null)
        {
            var participant = await context.Participants.FindAsync([participantId], cancellationToken);
            if (participant is null)
            {
                throw new ApplicationRuleException(ApplicationRuleError.Participant.NotFound);
            }
        
            record = new ParticipationView
            {
                ParticipantId = participantId,
                PublicId = participant.PublicId,
                TeamId = participant.TeamId,
                TicketedEventId = eventId
            };

            context.ParticipationView.Add(record);
        }

        return record;
    }
}