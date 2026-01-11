using Amolenk.Admitto.Application.Common.Messaging;

namespace Amolenk.Admitto.Application.UseCases.Email.SendReconfirmEmail;

/// <summary>
/// Represents a command to send a bulk of reconfirm emails.
/// </summary>
public record SendReconfirmEmailCommand(
    Guid TeamId,
    Guid TicketedEventId,
    Guid AttendeeId)
    : Command;
