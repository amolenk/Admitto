using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;

namespace Amolenk.Admitto.Core.Email.Domain.Entities;

public class EmailLog : Entity<EmailLogId>
{
    // Required for EF Core
    private EmailLog()
    {
    }

    private EmailLog(
        EmailLogId id,
        TeamId teamId,
        TicketedEventId ticketedEventId,
        string idempotencyKey,
        EmailAddress recipient,
        string emailType,
        string subject,
        string provider,
        string? providerMessageId,
        EmailLogStatus status,
        DateTimeOffset? sentAt,
        DateTimeOffset statusUpdatedAt,
        string? lastError,
        BulkEmailJobId? bulkEmailJobId,
        RegistrationId? registrationId)
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
        BulkEmailJobId = bulkEmailJobId;
        RegistrationId = registrationId;
    }

    public TeamId TeamId { get; private set; }
    public TicketedEventId TicketedEventId { get; private set; }
    public string IdempotencyKey { get; private set; } = default!;
    public EmailAddress Recipient { get; private set; }
    public string EmailType { get; private set; } = default!;
    public string Subject { get; private set; } = default!;
    public string Provider { get; private set; } = default!;
    public string? ProviderMessageId { get; private set; }
    public EmailLogStatus Status { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset StatusUpdatedAt { get; private set; }
    public string? LastError { get; private set; }

    /// <summary>
    /// When this log row was produced by a bulk-email fan-out, links back to
    /// the originating <see cref="BulkEmailJob"/>. <c>null</c> for single-send
    /// emails.
    /// </summary>
    public BulkEmailJobId? BulkEmailJobId { get; private set; }

    /// <summary>
    /// The registration associated with this email send, when applicable.
    /// <c>null</c> for external-list bulk sends and any send not tied to a
    /// specific registration.
    /// </summary>
    public RegistrationId? RegistrationId { get; private set; }

    public static EmailLog Create(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        string idempotencyKey,
        EmailAddress recipient,
        string emailType,
        string subject,
        string provider,
        string? providerMessageId,
        EmailLogStatus status,
        DateTimeOffset? sentAt,
        DateTimeOffset statusUpdatedAt,
        string? lastError = null,
        BulkEmailJobId? bulkEmailJobId = null,
        RegistrationId? registrationId = null)
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
            lastError,
            bulkEmailJobId,
            registrationId);
    }
}
