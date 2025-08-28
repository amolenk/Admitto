namespace Amolenk.Admitto.Application.Common.Email.Sending;

/// <summary>
/// Tracks sent emails to prevent duplication.
/// </summary>
public record EmailLog(
    Guid Id,
    Guid TicketedEventId, 
    Guid IdempotencyKey,
    string Recipient,
    string EmailType,
    string Subject,
    string Provider,
    string? ProviderMessageId,
    EmailStatus Status,
    DateTimeOffset? SentAt,
    DateTimeOffset StatusUpdatedAt,
    string? LastError);

public enum EmailStatus
{
    Sent,
    Delivered,
    Bounced,
    Failed
}