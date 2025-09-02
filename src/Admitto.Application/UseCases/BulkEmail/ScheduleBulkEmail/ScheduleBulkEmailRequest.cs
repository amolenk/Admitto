namespace Amolenk.Admitto.Application.UseCases.BulkEmail.ScheduleBulkEmail;

/// <summary>
/// Represents a request to schedule a bulk email job.
/// </summary>
public record ScheduleBulkEmailRequest(string EmailType, RepeatDto? Repeat);
    
public record RepeatDto(DateTimeOffset WindowStart, DateTimeOffset WindowEnd);