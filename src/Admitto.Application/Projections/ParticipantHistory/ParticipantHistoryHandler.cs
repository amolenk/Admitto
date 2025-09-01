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
    IApplicationEventHandler<EmailSentApplicationEvent>
{
    public async ValueTask HandleAsync(AttendeeRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        LogActivityAsync(
            domainEvent.TicketedEventId,
            domainEvent.ParticipantId,
            domainEvent.DomainEventId,
            $"✅ Registered",
            domainEvent.OccurredOn);
    }
    
    public async ValueTask HandleAsync(AttendeeCanceledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        LogActivityAsync(
            domainEvent.TicketedEventId,
            domainEvent.ParticipantId,
            domainEvent.DomainEventId,
            $"❌ Canceled registration (on time)",
            domainEvent.OccurredOn);
    }
    
    public async ValueTask HandleAsync(AttendeeCanceledLateDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        LogActivityAsync(
            domainEvent.TicketedEventId,
            domainEvent.ParticipantId,
            domainEvent.DomainEventId,
            $"🚨 Canceled registration (late)",
            domainEvent.OccurredOn);
    }

    public ValueTask HandleAsync(EmailSentApplicationEvent applicationEvent, CancellationToken cancellationToken)
    {
        if (applicationEvent.RecipientType != EmailRecipientType.Attendee)
        {
            return ValueTask.CompletedTask;
        }
        
        LogActivityAsync(
            applicationEvent.TicketedEventId,
            applicationEvent.ParticipantId,
            applicationEvent.ApplicationEventId,
            $"📧 {applicationEvent.Subject}",
            applicationEvent.OccurredOn,
            applicationEvent.EmailType.ToString(),
            applicationEvent.EmailLogId);
    
        return ValueTask.CompletedTask;
    }
    
    private void LogActivityAsync(
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