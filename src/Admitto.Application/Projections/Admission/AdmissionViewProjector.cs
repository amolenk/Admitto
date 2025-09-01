using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Domain.DomainEvents;
using Humanizer;

namespace Amolenk.Admitto.Application.Projections.Admission;

public class AdmissionViewProjector(IApplicationContext context)
    : IEventualDomainEventHandler<AttendeeRegisteredDomainEvent>,
        IEventualDomainEventHandler<AttendeeCanceledDomainEvent>,
        IEventualDomainEventHandler<AttendeeCanceledLateDomainEvent>,
        IEventualDomainEventHandler<ContributorAddedDomainEvent>,
        IEventualDomainEventHandler<ContributorUpdatedDomainEvent>,
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

            record.ContributorStatus = string.Join(", ", domainEvent.Roles.Select(r => r.Name.Humanize()));
            record.ContributorId = domainEvent.ContributorId;
            record.LastModifiedAt = domainEvent.OccurredOn;
        }
    }

    public async ValueTask HandleAsync(ContributorUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
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
                if (domainEvent.Email is not null)
                {
                    record.Email = domainEvent.Email;
                }

                if (domainEvent.FirstName is not null)
                {
                    record.FirstName = domainEvent.FirstName;
                }

                if (domainEvent.LastName is not null)
                {
                    record.LastName = domainEvent.LastName;
                }
            }

            if (domainEvent.Roles is not null)
            {
                record.ContributorStatus = string.Join(", ", domainEvent.Roles.Select(r => r.Name.Humanize()));
            }
            
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
                TicketedEventId = eventId
            };

            context.AdmissionView.Add(record);
        }

        return record;
    }
}