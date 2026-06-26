using Amolenk.Admitto.Core.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmails;

public sealed record BulkEmailListItemDto(
    Guid Id,
    string EmailType,
    BulkEmailJobStatus Status,
    int RecipientCount,
    int SentCount,
    int FailedCount,
    int CancelledCount,
    bool IsSystemTriggered,
    string? TriggeredBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancellationRequestedAt,
    DateTimeOffset? CancelledAt);
