using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.BulkEmail.ScheduleBulkEmail;

/// <summary>
/// Represents a command to schedule a bulk email job.
/// </summary>
public record ScheduleBulkEmailCommand(
    Guid TeamId,
    Guid TicketedEventId,
    string EmailType,
    BulkEmailWorkItemRepeat? Repeat)
    : Command;
