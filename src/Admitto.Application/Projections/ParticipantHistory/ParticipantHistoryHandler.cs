using Amolenk.Admitto.Application.Common.Core;
using Amolenk.Admitto.Application.Common.Email.Sending;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;
using Humanizer;

namespace Amolenk.Admitto.Application.Projections.ParticipantHistory;

public class ParticipantHistoryHandler(IApplicationContext context)
    : IEventualDomainEventHandler<AttendeeRegisteredDomainEvent>,
    IEventualDomainEventHandler<AttendeeCanceledDomainEvent>,
    IEventualDomainEventHandler<AttendeeCanceledLateDomainEvent>,
    IEventualDomainEventHandler<AttendeeReconfirmedDomainEvent>,
    IApplicationEventHandler<EmailSentApplicationEvent>
{
    public ValueTask HandleAsync(AttendeeRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        LogActivity(
            domainEvent.TicketedEventId,
            domainEvent.ParticipantId,
            domainEvent.DomainEventId,
            $"✅ Registered",
            domainEvent.OccurredOn);
        
        return ValueTask.CompletedTask;
    }
    
    public ValueTask HandleAsync(AttendeeCanceledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        LogActivity(
            domainEvent.TicketedEventId,
            domainEvent.ParticipantId,
            domainEvent.DomainEventId,
            $"❌ Canceled registration (on time)",
            domainEvent.OccurredOn);
        
        return ValueTask.CompletedTask;
    }
    
    public ValueTask HandleAsync(AttendeeCanceledLateDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        LogActivity(
            domainEvent.TicketedEventId,
            domainEvent.ParticipantId,
            domainEvent.DomainEventId,
            $"🚨 Canceled registration (late)",
            domainEvent.OccurredOn);
        
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(AttendeeReconfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        LogActivity(
            domainEvent.TicketedEventId,
            domainEvent.ParticipantId,
            domainEvent.DomainEventId,
            $"🔄 Reconfirmed registration",
            domainEvent.OccurredOn);
        
        return ValueTask.CompletedTask;
    }
    
    public ValueTask HandleAsync(EmailSentApplicationEvent applicationEvent, CancellationToken cancellationToken)
    {
        LogActivity(
            applicationEvent.TicketedEventId,
            applicationEvent.ParticipantId,
            applicationEvent.ApplicationEventId,
            $"📧 {applicationEvent.Subject}",
            applicationEvent.OccurredOn,
            applicationEvent.EmailType.ToString(),
            applicationEvent.EmailLogId);
    
        return ValueTask.CompletedTask;
    }
    
    private void LogActivity(
        Guid ticketedEventId,
        Guid participantId,
        Guid sourceId,
        string activity,
        DateTimeOffset occurredAt,
        string? emailType = null,
        Guid? emailLogId = null)
    {
        var record = new ParticipantHistoryView
        {
            Id = Guid.NewGuid(),
            TicketedEventId = ticketedEventId,
            ParticipantId = participantId,
            SourceId = sourceId,
            Activity = activity.Truncate(255),
            EmailLogId = emailLogId,
            OccuredAt = occurredAt
        };
    
        context.AttendeeActivityView.Add(record);
    }
}