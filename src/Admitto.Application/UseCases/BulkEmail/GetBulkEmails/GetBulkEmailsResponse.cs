using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.BulkEmail.GetBulkEmails;

public record GetBulkEmailsResponse(BulkEmailWorkItemDto[] BulkEmails);

public record BulkEmailWorkItemDto(
    Guid Id,
    string EmailType,
    BulkEmailWorkItemRepeatDto? Repeat,
    BulkEmailWorkItemStatus Status,
    DateTimeOffset? LastRunAt,
    string? Error);

public record BulkEmailWorkItemRepeatDto(DateTimeOffset WindowStart, DateTimeOffset WindowEnd);