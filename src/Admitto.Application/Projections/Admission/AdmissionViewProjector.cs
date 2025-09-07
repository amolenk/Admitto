using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Projections.Admission;

public class AdmissionViewProjector(IApplicationContext context)
    : IEventualDomainEventHandler<AttendeeRegisteredDomainEvent>,
        IEventualDomainEventHandler<AttendeeCanceledDomainEvent>,
        IEventualDomainEventHandler<AttendeeCanceledLateDomainEvent>,
        IEventualDomainEventHandler<ContributorAddedDomainEvent>,
        IEventualDomainEventHandler<ContributorRolesChangedDomainEvent>,
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
            record.AttendeeStatus = "Registered";
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
            record.AttendeeStatus = "Canceled";
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
            record.AttendeeStatus = "Canceled";
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

            record.ContributorStatus = string.Join(", ", domainEvent.Roles.Select(r => r.ToString()));
            record.ContributorId = domainEvent.ContributorId;
            record.LastModifiedAt = domainEvent.OccurredOn;
        }
    }

    public async ValueTask HandleAsync(ContributorRolesChangedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var record = await GetOrCreateRecordAsync(
            domainEvent.ParticipantId,
            domainEvent.TicketedEventId,
            cancellationToken);

        if (domainEvent.OccurredOn > record.LastModifiedAt)
        {
            record.ContributorStatus = string.Join(", ", domainEvent.CurrentRoles.Select(r => r.ToString()));
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
            record.ContributorStatus = "Removed";
            record.ContributorId = null;
            record.LastModifiedAt = domainEvent.OccurredOn;
        }
    }

    private async ValueTask<AdmissionView> GetOrCreateRecordAsync(
        Guid participantId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var record = await context.AdmissionView.FindAsync(
            [participantId],
            cancellationToken);

        if (record is null)
        {
            var participant = await context.Participants.FindAsync([participantId], cancellationToken);
            if (participant is null)
            {
                throw new ApplicationRuleException(ApplicationRuleError.Participant.NotFound);
            }
        
            record = new AdmissionView
            {
                ParticipantId = participantId,
                PublicId = participant.PublicId,
                TeamId = participant.TeamId,
                TicketedEventId = eventId
            };

            context.AdmissionView.Add(record);
        }

        return record;
    }
}