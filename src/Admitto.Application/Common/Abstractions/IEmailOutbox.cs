using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IEmailOutbox
{
    ValueTask EnqueueEmailAsync(string recipientEmail, EmailTemplateId templateId, 
        Dictionary<string, string> templateParameters, TicketedEventId ticketedEventId, bool priority = false,
        CancellationToken cancellationToken = default);
}