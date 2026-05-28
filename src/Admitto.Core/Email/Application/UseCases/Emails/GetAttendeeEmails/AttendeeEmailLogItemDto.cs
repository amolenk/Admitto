namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.GetAttendeeEmails;

public sealed record AttendeeEmailLogItemDto(
    Guid Id,
    string Subject,
    string EmailType,
    string Status,
    DateTimeOffset? SentAt,
    Guid? BulkEmailJobId);
