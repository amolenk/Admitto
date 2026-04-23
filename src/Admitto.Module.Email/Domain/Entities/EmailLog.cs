using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;

namespace Amolenk.Admitto.Module.Email.Domain.Entities;

public class EmailLog : Entity<EmailLogId>
{
    // Required for EF Core
    private EmailLog()
    {
    }

    private EmailLog(
        EmailLogId id,
        Guid teamId,
        Guid ticketedEventId,
        string idempotencyKey,
        string recipient,
        string emailType,
        string subject,
        string provider,
        string? providerMessageId,
        EmailLogStatus status,
        DateTimeOffset? sentAt,
        DateTimeOffset statusUpdatedAt,
        string? lastError)
        : base(id)
    {
        TeamId = teamId;
        TicketedEventId = ticketedEventId;
        IdempotencyKey = idempotencyKey;
        Recipient = recipient;
        EmailType = emailType;
        Subject = subject;
        Provider = provider;
        ProviderMessageId = providerMessageId;
        Status = status;
        SentAt = sentAt;
        StatusUpdatedAt = statusUpdatedAt;
        LastError = lastError;
    }

    public Guid TeamId { get; private set; }
    public Guid TicketedEventId { get; private set; }
    public string IdempotencyKey { get; private set; } = default!;
    public string Recipient { get; private set; } = default!;
    public string EmailType { get; private set; } = default!;
    public string Subject { get; private set; } = default!;
    public string Provider { get; private set; } = default!;
    public string? ProviderMessageId { get; private set; }
    public EmailLogStatus Status { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset StatusUpdatedAt { get; private set; }
    public string? LastError { get; private set; }

    public static EmailLog Create(
        Guid teamId,
        Guid ticketedEventId,
        string idempotencyKey,
        string recipient,
        string emailType,
        string subject,
        string provider,
        string? providerMessageId,
        EmailLogStatus status,
        DateTimeOffset? sentAt,
        DateTimeOffset statusUpdatedAt,
        string? lastError = null)
    {
        return new EmailLog(
            EmailLogId.New(),
            teamId,
            ticketedEventId,
            idempotencyKey,
            recipient,
            emailType,
            subject,
            provider,
            providerMessageId,
            status,
            sentAt,
            statusUpdatedAt,
            lastError);
    }
}
