using Amolenk.Admitto.Application.Common.Email.Sending;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Email;

/// <summary>
/// Classes that implement this interface can dispatch emails.
/// </summary>
public interface IEmailDispatcher
{
    // TODO Replace with string key
    ValueTask DispatchEmailAsync(
        EmailMessage emailMessage,
        Guid teamId,
        Guid ticketedEventId,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default);

    // TODO Remove in favor of string overload
    ValueTask DispatchEmailsAsync(
        IAsyncEnumerable<EmailMessage> emailMessages,
        Guid teamId,
        Guid ticketedEventId,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default);

    ValueTask DispatchEmailsAsync(
        IAsyncEnumerable<EmailMessage> emailMessages,
        Guid teamId,
        Guid ticketedEventId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Dispatches emails to recipients.
/// Dispatching consists of:
/// 1. Checking if the email has already been sent for a given idempotency key.
/// 2. If not sent, sending the email.
/// 3. Logging the email in the database.
/// 4. Raising an EmailSent application event.
/// </summary>
/// <remarks>
/// The idempotency key is used to ensure that an email is sent only once for a specific context (e.g. a registration).
/// Note that this class is not thread-safe. Multiple concurrent calls with the same idempotency key may result in
/// duplicate emails.
/// </remarks>
public class EmailDispatcher(
    IEmailSenderFactory emailSenderFactory,
    IApplicationContext context,
    IMessageOutbox messageOutbox,
    IUnitOfWork unitOfWork,
    ILogger<EmailDispatcher> logger) : IEmailDispatcher
{
    public static readonly Guid TestMessageIdempotencyKey = Guid.Empty;

    public async ValueTask DispatchEmailAsync(
        EmailMessage emailMessage,
        Guid teamId,
        Guid ticketedEventId,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        using var emailSender = await GetEmailSenderAsync(teamId);

        await SendEmailInternalAsync(
            emailMessage,
            emailSender,
            teamId,
            ticketedEventId,
            idempotencyKey,
            cancellationToken);
    }

    public ValueTask DispatchEmailsAsync(
        IAsyncEnumerable<EmailMessage> emailMessages,
        Guid teamId,
        Guid ticketedEventId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return DispatchEmailsAsync(
            emailMessages,
            teamId,
            ticketedEventId,
            DeterministicGuid.Create(idempotencyKey),
            cancellationToken);
    }

    public async ValueTask DispatchEmailsAsync(
        IAsyncEnumerable<EmailMessage> emailMessages,
        Guid teamId,
        Guid ticketedEventId,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        using var emailSender = await GetEmailSenderAsync(teamId);

        await foreach (var emailMessage in emailMessages.WithCancellation(cancellationToken))
        {
            await SendEmailInternalAsync(
                emailMessage,
                emailSender,
                teamId,
                ticketedEventId,
                idempotencyKey,
                cancellationToken);

            // Commit after each email to avoid losing progress in case of an error
            await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

            // Throttle to lower performance impact on the system and external email service
            await Task.Delay(500, cancellationToken);
        }
    }

    private async ValueTask<IEmailSender> GetEmailSenderAsync(Guid teamId)
    {
        var team = await context.Teams
            .AsNoTracking()
            .Where(t => t.Id == teamId)
            .Select(t => new { t.Name, t.Email, t.EmailServiceConnectionString })
            .FirstOrDefaultAsync();

        if (team == null)
        {
            throw new ArgumentException($"No email settings found for team '{teamId}'");
        }

        return await emailSenderFactory.GetEmailSenderAsync(
            team.Name,
            team.Email,
            team.EmailServiceConnectionString);
    }

    private async ValueTask SendEmailInternalAsync(
        EmailMessage emailMessage,
        IEmailSender emailSender,
        Guid teamId,
        Guid ticketedEventId,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        // If this is a test email message, send it without checking for duplicates or logging the result.
        if (idempotencyKey == TestMessageIdempotencyKey)
        {
            await emailSender.SendEmailAsync(emailMessage);
            return;
        }

        var emailSent = await context.EmailLog
            .Where(l => l.TicketedEventId == ticketedEventId && l.Recipient == emailMessage.Recipient &&
                        l.IdempotencyKey == idempotencyKey)
            .AnyAsync(cancellationToken);

        if (emailSent)
        {
            logger.LogInformation(
                "Skipping already sent email with subject '{Subject}' to '{Recipient}' for idempotency key '{IdempotencyKey}'.",
                emailMessage.Subject,
                emailMessage.Recipient,
                idempotencyKey);

            return;
        }

        await emailSender.SendEmailAsync(emailMessage);

        logger.LogInformation(
            "Sent email with subject '{Subject}' to '{Recipient}' for idempotency key '{IdempotencyKey}'.",
            emailMessage.Subject,
            emailMessage.Recipient,
            idempotencyKey);

        // Directly log the email in the database for strong consistency.
        var now = DateTimeOffset.UtcNow;
        var emailLog = new EmailLog
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            TicketedEventId = ticketedEventId,
            IdempotencyKey = idempotencyKey,
            Recipient = emailMessage.Recipient,
            EmailType = emailMessage.EmailType,
            Subject = emailMessage.Subject,
            Provider = emailSender.GetType().Name, // TODO Let IEmailSender provide its name
            Status = EmailStatus.Sent,
            SentAt = now,
            StatusUpdatedAt = now
        };
        context.EmailLog.Add(emailLog);

        // If the email is for an existing participant, raise an application event to notify other parts of the system.
        if (emailMessage.ParticipantId is not null)
        {
            messageOutbox.Enqueue(
                new EmailSentApplicationEvent(
                    teamId,
                    ticketedEventId,
                    emailMessage.ParticipantId.Value,
                    emailMessage.Recipient,
                    emailMessage.Subject,
                    emailMessage.EmailType,
                    emailLog.Id));
        }
    }
}