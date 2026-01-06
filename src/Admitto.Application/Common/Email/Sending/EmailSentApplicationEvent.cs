using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Email.Sending;

/// <summary>
/// Represents an event that is triggered when an email has been sent.
/// </summary>
public record EmailSentApplicationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid ParticipantId,
    string Recipient,
    string Subject,
    string EmailType,
    Guid EmailLogId)
    : ApplicationEvent;
