using Amolenk.Admitto.Application.Common.Messaging;

namespace Amolenk.Admitto.Application.UseCases.BulkEmail.ScheduleReconfirmBulkEmail;

/// <summary>
/// Represents a command to schedule a reconfirmation bulk email for a ticketed event.
/// </summary>
public record ScheduleReconfirmBulkEmailCommand(Guid TeamId, Guid TicketedEventId) : Command;