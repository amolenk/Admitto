namespace Amolenk.Admitto.Application.Common.Email.Sending;

/// <summary>
/// Tracks sent emails to prevent duplication.
/// </summary>
public record EmailLog(Guid Id, Guid TicketedEventId, Guid IdempotencyKey, string Email, DateTimeOffset CreatedAt);
