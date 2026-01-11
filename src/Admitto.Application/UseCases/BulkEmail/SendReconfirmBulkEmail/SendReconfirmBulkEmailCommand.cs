using Amolenk.Admitto.Application.Common.Messaging;

namespace Amolenk.Admitto.Application.UseCases.BulkEmail.SendReconfirmBulkEmail;

/// <summary>
/// Represents a command to send a bulk of reconfirm emails.
/// </summary>
public record SendReconfirmBulkEmailCommand(
    Guid TeamId,
    Guid TicketedEventId,
    TimeSpan InitialDelayAfterRegistration,
    TimeSpan? ReminderInterval)
    : Command;
