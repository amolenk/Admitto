using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IEmailOutbox
{
    ValueTask EnqueueEmailAsync(string recipientEmail, string subject, string templateId, 
        Dictionary<string, string> templateParameters, TeamId teamId, TicketedEventId? ticketedEventId = null,
        AttendeeId? attendeeId = null, bool priority = false);
}