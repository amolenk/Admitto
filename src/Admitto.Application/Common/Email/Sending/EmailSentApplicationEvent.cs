using Amolenk.Admitto.Application.Common.Core;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Email.Sending;

/// <summary>
/// Represents an event that is triggered when an email has been sent.
/// </summary>
public record EmailSentApplicationEvent(
    Guid TicketedEventId,
    string Recipient,
    string Subject,
    EmailType EmailType,
    Guid EmailLogId)
    : ApplicationEvent;
