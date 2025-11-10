using Amolenk.Admitto.Application.Common.Core;
using Amolenk.Admitto.Application.Common.Email.Sending;
using Amolenk.Admitto.Domain;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Projections.ParticipantActivity;

public class ParticipantActivityHandler(IApplicationContext context)
    : IEventualDomainEventHandler<AttendeeRegisteredDomainEvent>,
    IEventualDomainEventHandler<AttendeeCanceledDomainEvent>,
    IEventualDomainEventHandler<AttendeeCanceledLateDomainEvent>,
    IEventualDomainEventHandler<AttendeeReconfirmedDomainEvent>,
    IEventualDomainEventHandler<AttendeeTicketsChangedDomainEvent>,
    IEventualDomainEventHandler<ContributorAddedDomainEvent>,
    IEventualDomainEventHandler<ContributorRolesChangedDomainEvent>,
    IEventualDomainEventHandler<ContributorRemovedDomainEvent>,
    IApplicationEventHandler<EmailSentApplicationEvent>
{
    public ValueTask HandleAsync(AttendeeRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        LogActivity(
            domainEvent.TicketedEventId,
            domainEvent.ParticipantId,
            domainEvent.DomainEventId,
            ParticipantActivity.Registered,
            domainEvent.OccurredOn);
        
        return ValueTask.CompletedTask;
    }
    
    public ValueTask HandleAsync(AttendeeCanceledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var activity = domainEvent.Reason switch
        {
            CancellationReason.TicketTypeRemoved => ParticipantActivity.CanceledDueToTicketTypeRemoval,
            CancellationReason.VisaLetterDenied => ParticipantActivity.CanceledDueToVisaLetterDenial,
            _ => ParticipantActivity.CanceledOnTime
        };
        
        LogActivity(
            domainEvent.TicketedEventId,
            domainEvent.ParticipantId,
            domainEvent.DomainEventId,
            activity,
            domainEvent.OccurredOn);
        
        return ValueTask.CompletedTask;
    }
    
    public ValueTask HandleAsync(AttendeeCanceledLateDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        LogActivity(
            domainEvent.TicketedEventId,
            domainEvent.ParticipantId,
            domainEvent.DomainEventId,
            ParticipantActivity.CanceledLate,
            domainEvent.OccurredOn);
        
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(AttendeeReconfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        LogActivity(
            domainEvent.TicketedEventId,
            domainEvent.ParticipantId,
            domainEvent.DomainEventId,
            ParticipantActivity.Reconfirmed,
            domainEvent.OccurredOn);
        
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(AttendeeTicketsChangedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        LogActivity(
            domainEvent.TicketedEventId,
            domainEvent.ParticipantId,
            domainEvent.DomainEventId,
            ParticipantActivity.TicketSelectionChanged,
            domainEvent.OccurredOn);
        
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(ContributorAddedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        foreach (var role in domainEvent.Roles)
        {
            LogContributorRoleAddedActivity(
                domainEvent.TicketedEventId,
                domainEvent.ParticipantId,
                domainEvent.DomainEventId,
                role,
                domainEvent.OccurredOn);
        }

        return ValueTask.CompletedTask;
    }
    
    public ValueTask HandleAsync(ContributorRolesChangedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var removedRoles = domainEvent.PreviousRoles.Except(domainEvent.CurrentRoles).ToList();
        foreach (var role in removedRoles)
        {
            LogContributorRoleRemovedActivity(
                domainEvent.TicketedEventId,
                domainEvent.ParticipantId,
                domainEvent.DomainEventId,
                role,
                domainEvent.OccurredOn);
        }
        
        var addedRoles = domainEvent.CurrentRoles.Except(domainEvent.PreviousRoles).ToList();
        foreach (var role in addedRoles)
        {
            LogContributorRoleAddedActivity(
                domainEvent.TicketedEventId,
                domainEvent.ParticipantId,
                domainEvent.DomainEventId,
                role,
                domainEvent.OccurredOn);
        }

        return ValueTask.CompletedTask;
    }
    
    public ValueTask HandleAsync(ContributorRemovedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        foreach (var role in domainEvent.Roles)
        {
            LogContributorRoleRemovedActivity(
                domainEvent.TicketedEventId,
                domainEvent.ParticipantId,
                domainEvent.DomainEventId,
                role,
                domainEvent.OccurredOn);
        }

        return ValueTask.CompletedTask;
    }
    
    public ValueTask HandleAsync(EmailSentApplicationEvent applicationEvent, CancellationToken cancellationToken)
    {
        LogActivity(
            applicationEvent.TicketedEventId,
            applicationEvent.ParticipantId,
            applicationEvent.ApplicationEventId,
            ParticipantActivity.EmailSent,
            applicationEvent.OccurredOn,
            applicationEvent.EmailLogId);
    
        return ValueTask.CompletedTask;
    }
    
    private void LogContributorRoleAddedActivity(
        Guid ticketedEventId,
        Guid participantId,
        Guid sourceId,
        ContributorRole role,
        DateTimeOffset occurredOn)
    {
        var activity = role switch
        {
            ContributorRole.Crew => ParticipantActivity.CrewAdded,
            ContributorRole.Speaker => ParticipantActivity.SpeakerAdded,
            ContributorRole.Sponsor => ParticipantActivity.SponsorAdded,
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };

        LogActivity(
            ticketedEventId,
            participantId,
            sourceId,
            activity,
            occurredOn);
    }
    
    private void LogContributorRoleRemovedActivity(
        Guid ticketedEventId,
        Guid participantId,
        Guid sourceId,
        ContributorRole role,
        DateTimeOffset occurredOn)
    {
        var activity = role switch
        {
            ContributorRole.Crew => ParticipantActivity.CrewRemoved,
            ContributorRole.Speaker => ParticipantActivity.SpeakerRemoved,
            ContributorRole.Sponsor => ParticipantActivity.SponsorRemoved,
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
        
        LogActivity(
            ticketedEventId,
            participantId,
            sourceId,
            activity,
            occurredOn);
    }
    
    private void LogActivity(
        Guid ticketedEventId,
        Guid participantId,
        Guid sourceId,
        ParticipantActivity activity,
        DateTimeOffset occurredAt,
        Guid? emailLogId = null)
    {
        var record = new ParticipantActivityView
        {
            Id = Guid.NewGuid(),
            TicketedEventId = ticketedEventId,
            ParticipantId = participantId,
            SourceId = sourceId,
            Activity = activity,
            EmailLogId = emailLogId,
            OccuredOn = occurredAt
        };
    
        context.ParticipantActivityView.Add(record);
    }


}