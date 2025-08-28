using Amolenk.Admitto.Application.Common.Core;
using Amolenk.Admitto.Application.Common.Email.Sending;
using Amolenk.Admitto.Domain.DomainEvents;
using Humanizer;

namespace Amolenk.Admitto.Application.Projections.ParticipantActivity;

public class ParticipantActivityHandler(IApplicationContext context)
    : IEventualDomainEventHandler<AttendeeRegisteredDomainEvent>,
        IEventualDomainEventHandler<AttendeeCanceledDomainEvent>,
        IEventualDomainEventHandler<AttendeeCanceledLateDomainEvent>,
        IEventualDomainEventHandler<SpeakerEngagementAddedDomainEvent>,
        IEventualDomainEventHandler<CrewAssignmentAddedDomainEvent>,
        IApplicationEventHandler<EmailSentApplicationEvent>
{
    public async ValueTask HandleAsync(AttendeeRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventName = await GetEventNameAsync(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            cancellationToken);

        AddActivityAsync(
            domainEvent.TicketedEventId,
            domainEvent.Email,
            domainEvent.DomainEventId,
            $"✅ Registered for {eventName}",
            domainEvent.OccurredOn);
    }

    public async ValueTask HandleAsync(AttendeeCanceledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventName = await GetEventNameAsync(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            cancellationToken);

        AddActivityAsync(
            domainEvent.TicketedEventId,
            domainEvent.Email,
            domainEvent.DomainEventId,
            $"❌ Canceled {eventName} registration (on time)",
            domainEvent.OccurredOn);
    }

    public async ValueTask HandleAsync(AttendeeCanceledLateDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventName = await GetEventNameAsync(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            cancellationToken);

        AddActivityAsync(
            domainEvent.TicketedEventId,
            domainEvent.Email,
            domainEvent.DomainEventId,
            $"🚨 Canceled {eventName} registration (late)",
            domainEvent.OccurredOn);
    }

    public async ValueTask HandleAsync(
        SpeakerEngagementAddedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var eventName = await GetEventNameAsync(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            cancellationToken);

        AddActivityAsync(
            domainEvent.TicketedEventId,
            domainEvent.Email,
            domainEvent.DomainEventId,
            $"🎤 Speaker engagement for {eventName}",
            domainEvent.OccurredOn);
    }

    public async ValueTask HandleAsync(CrewAssignmentAddedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventName = await GetEventNameAsync(
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            cancellationToken);

        AddActivityAsync(
            domainEvent.TicketedEventId,
            domainEvent.Email,
            domainEvent.DomainEventId,
            $"👷 Crew assignment for {eventName}",
            domainEvent.OccurredOn);
    }

    public ValueTask HandleAsync(EmailSentApplicationEvent applicationEvent, CancellationToken cancellationToken)
    {
        AddActivityAsync(
            applicationEvent.TicketedEventId,
            applicationEvent.Recipient,
            applicationEvent.ApplicationEventId,
            $"📧 {applicationEvent.Subject}",
            applicationEvent.OccurredOn,
            applicationEvent.EmailLogId);

        return ValueTask.CompletedTask;
    }

    private void AddActivityAsync(
        Guid ticketedEventId,
        string email,
        Guid sourceId,
        string activity,
        DateTimeOffset occurredAt,
        Guid? emailLogId = null)
    {
        var record = new ParticipantActivityView
        {
            TicketedEventId = ticketedEventId,
            Email = email,
            SourceId = sourceId,
            Activity = activity.Truncate(255),
            EmailLogId = emailLogId,
            OccuredAt = occurredAt
        };

        context.ParticipantActivityView.Add(record);
    }

    private async ValueTask<string> GetEventNameAsync(
        Guid teamId,
        Guid ticketedEventId,
        CancellationToken cancellationToken)
    {
        var eventName = await context.TicketedEvents
            .AsNoTracking()
            .Where(e => e.TeamId == teamId && e.Id == ticketedEventId)
            .Select(e => e.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return eventName ?? ticketedEventId.ToString();
    }
}