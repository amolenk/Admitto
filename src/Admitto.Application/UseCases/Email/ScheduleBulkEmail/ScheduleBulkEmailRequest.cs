namespace Amolenk.Admitto.Application.UseCases.Email.ScheduleBulkEmail;

/// <summary>
/// Represents a request to schedule a bulk email job.
/// </summary>
public record ScheduleBulkEmailRequest(
    string EmailType,
    DateTimeOffset EarliestSendTime,
    DateTimeOffset LatestSendTime);