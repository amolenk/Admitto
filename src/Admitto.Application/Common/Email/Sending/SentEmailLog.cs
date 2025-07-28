namespace Amolenk.Admitto.Application.Common.Email.Sending;

/// <summary>
/// Represents a log entry for an email that has been sent.
/// </summary>
public record SentEmailLog(Guid Id, Guid TicketedEventId, Guid DispatchId, string Email);
