using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email.Sending;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Email;

/// <summary>
/// Classes that implement this interface can dispatch emails.
/// </summary>
public interface IEmailDispatcher
{
    ValueTask DispatchEmailAsync(
        EmailMessage emailMessage,
        Guid teamId,
        Guid ticketedEventId,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default);

    ValueTask DispatchEmailsAsync(
        IAsyncEnumerable<EmailMessage> emailMessages,
        Guid teamId,
        Guid ticketedEventId,
        Guid idempotencyKey,
        CancellationToken cancellationToken);
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
/// </remarks>
public class EmailDispatcher(
    ITeamConfigEncryptionService encryptionService,
    IEmailSenderFactory emailSenderFactory,
    IApplicationContext context,
    IMessageOutbox messageOutbox,
    IUnitOfWork unitOfWork,
    ILogger<EmailDispatcher> logger) : IEmailDispatcher
{
    public static readonly Guid TestMessageDispatchId = Guid.Empty;

    public async ValueTask DispatchEmailAsync(
        EmailMessage emailMessage,
        Guid teamId,
        Guid ticketedEventId,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        using var emailSender = await GetEmailSenderAsync(teamId);

        await SendEmailInternalAsync(emailMessage, emailSender, ticketedEventId, idempotencyKey, cancellationToken);
    }

    public async ValueTask DispatchEmailsAsync(
        IAsyncEnumerable<EmailMessage> emailMessages,
        Guid teamId,
        Guid ticketedEventId,
        Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var emailSender = await GetEmailSenderAsync(teamId);

        await foreach (var emailMessage in emailMessages.WithCancellation(cancellationToken))
        {
            await SendEmailInternalAsync(emailMessage, emailSender, ticketedEventId, idempotencyKey, cancellationToken);

            // Commit after each email to avoid losing progress in case of an error
            await unitOfWork.SaveChangesAsync(cancellationToken);
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
            encryptionService.Decrypt(team.EmailServiceConnectionString));
    }

    private async ValueTask SendEmailInternalAsync(
        EmailMessage emailMessage,
        IEmailSender emailSender,
        Guid ticketedEventId,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        // If this is a test email message, send it without checking for duplicates or logging the result.
        if (idempotencyKey == TestMessageDispatchId)
        {
            await emailSender.SendEmailAsync(emailMessage);
            return;
        }

        var sentEmailLog = await context.SentEmailLogs.FirstOrDefaultAsync(
            l => l.TicketedEventId == ticketedEventId && l.Email == emailMessage.Recipient &&
                 l.IdempotencyKey == idempotencyKey,
            cancellationToken);

        if (sentEmailLog is not null)
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
            "Sent email with subject '{Subject}' to '{Recipient}' for dispatch ID '{DispatchId}'.",
            emailMessage.Subject,
            emailMessage.Recipient,
            idempotencyKey);

        // Directly log the email in the database for strong consistency.
        context.SentEmailLogs.Add(
            new SentEmailLog(
                Guid.NewGuid(),
                ticketedEventId,
                idempotencyKey,
                emailMessage.Recipient,
                DateTimeOffset.UtcNow));

        // Raise an application event to notify other parts of the system.
        messageOutbox.Enqueue(
            new EmailSentApplicationEvent(ticketedEventId, emailMessage.Recipient, emailMessage.EmailType));
    }
}