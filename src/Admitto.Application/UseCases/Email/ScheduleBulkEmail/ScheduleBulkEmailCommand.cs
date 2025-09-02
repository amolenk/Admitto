namespace Amolenk.Admitto.Application.UseCases.Email.ScheduleBulkEmail;

/// <summary>
/// Represents a command to schedule a bulk email job.
/// </summary>
public record ScheduleBulkEmailCommand(
    Guid TeamId,
    Guid TicketedEventId,
    string EmailType,
    DateTimeOffset EarliestSendTime,
    DateTimeOffset LatestSendTime)
    : Command;
