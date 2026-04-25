using Amolenk.Admitto.Module.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.GetBulkEmail;

public sealed record BulkEmailJobDetailDto(
    Guid Id,
    Guid TeamId,
    Guid TicketedEventId,
    string EmailType,
    string? Subject,
    string? TextBody,
    string? HtmlBody,
    BulkEmailJobSource Source,
    BulkEmailJobStatus Status,
    int RecipientCount,
    int SentCount,
    int FailedCount,
    int CancelledCount,
    string? LastError,
    bool IsSystemTriggered,
    string? TriggeredBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancellationRequestedAt,
    DateTimeOffset? CancelledAt,
    uint Version,
    IReadOnlyList<BulkEmailRecipientDto> Recipients);

public sealed record BulkEmailRecipientDto(
    string Email,
    string? DisplayName,
    Guid? RegistrationId,
    BulkEmailRecipientStatus Status,
    string? LastError);
