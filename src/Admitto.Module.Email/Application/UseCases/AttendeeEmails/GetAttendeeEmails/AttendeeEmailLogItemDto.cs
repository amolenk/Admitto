namespace Amolenk.Admitto.Module.Email.Application.UseCases.AttendeeEmails.GetAttendeeEmails;

public sealed record AttendeeEmailLogItemDto(
    Guid Id,
    string Subject,
    string EmailType,
    string Status,
    DateTimeOffset? SentAt,
    Guid? BulkEmailJobId);
