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
        TeamId? teamId,
        TicketedEventId? ticketedEventId,
        string idempotencyKey,
        EmailAddress recipient,
        string emailType,
        string subject,
        EmailLogStatus status,
        DateTimeOffset? sentAt,
        DateTimeOffset statusUpdatedAt,
        string? lastError,
        int deliveryAttemptCount,
        BulkEmailJobId? bulkEmailJobId,
        RegistrationId? registrationId,
        RegistrationCycleId? registrationCycleId)
        : base(id)
    {
        TeamId = teamId;
        TicketedEventId = ticketedEventId;
        IdempotencyKey = idempotencyKey;
        Recipient = recipient;
        EmailType = emailType;
        Subject = subject;
        Status = status;
        SentAt = sentAt;
        StatusUpdatedAt = statusUpdatedAt;
        LastError = lastError;
        DeliveryAttemptCount = deliveryAttemptCount;
        BulkEmailJobId = bulkEmailJobId;
        RegistrationId = registrationId;
        RegistrationCycleId = registrationCycleId;
    }

    public TeamId? TeamId { get; private set; }
    public TicketedEventId? TicketedEventId { get; private set; }
    public string IdempotencyKey { get; private set; } = default!;
    public EmailAddress Recipient { get; private set; }
    public string EmailType { get; private set; } = default!;
    public string Subject { get; private set; } = default!;
    public EmailLogStatus Status { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset StatusUpdatedAt { get; private set; }
    public string? LastError { get; private set; }
    public int DeliveryAttemptCount { get; private set; }

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
    public RegistrationCycleId? RegistrationCycleId { get; private set; }

    public static EmailLog Create(
        TeamId? teamId,
        TicketedEventId? ticketedEventId,
        string idempotencyKey,
        EmailAddress recipient,
        string emailType,
        string subject,
        EmailLogStatus status,
        DateTimeOffset? sentAt,
        DateTimeOffset statusUpdatedAt,
        string? lastError = null,
        int deliveryAttemptCount = 0,
        BulkEmailJobId? bulkEmailJobId = null,
        RegistrationId? registrationId = null,
        RegistrationCycleId? registrationCycleId = null)
    {
        return new EmailLog(
            EmailLogId.New(),
            teamId,
            ticketedEventId,
            idempotencyKey,
            recipient,
            emailType,
            subject,
            status,
            sentAt,
            statusUpdatedAt,
            lastError,
            deliveryAttemptCount,
            bulkEmailJobId,
            registrationId,
            registrationCycleId);
    }

    public bool IsTerminal => Status is EmailLogStatus.Sent or EmailLogStatus.Delivered or EmailLogStatus.Failed or EmailLogStatus.Bounced;

    public void MarkSent(string subject, DateTimeOffset sentAt)
    {
        Subject = subject;
        Status = EmailLogStatus.Sent;
        SentAt = sentAt;
        StatusUpdatedAt = sentAt;
        LastError = null;
    }

    public void MarkFailed(string subject, string error, DateTimeOffset failedAt)
    {
        Subject = subject;
        Status = EmailLogStatus.Failed;
        SentAt = null;
        StatusUpdatedAt = failedAt;
        LastError = error;
    }

    public void MarkRetryableFailure(string subject, string error, DateTimeOffset failedAt)
    {
        Subject = subject;
        Status = EmailLogStatus.Pending;
        SentAt = null;
        StatusUpdatedAt = failedAt;
        LastError = error;
        DeliveryAttemptCount++;
    }
}
