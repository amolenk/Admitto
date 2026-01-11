using System.Runtime.CompilerServices;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Data;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Composing;
using Amolenk.Admitto.Application.Common.Email.Templating;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Application.Jobs.SendReconfirmBulkEmail;
using Amolenk.Admitto.Application.Projections.Participation;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.BulkEmail.SendReconfirmBulkEmail;

/// <summary>
/// Represents a handler that sends reconfirmation emails in bulk to attendees of a ticketed event.
/// Reconfirmation emails are sent to attendees who have registered but have not yet reconfirmed their attendance.
/// </summary>
[RequiresCapability(HostCapability.Email)]
public class SendReconfirmBulkEmailHandler(
    IApplicationContext context,
    ISigningService signingService,
    IEmailTemplateService emailTemplateService,
    IEmailDispatcher emailDispatcher,
    ILogger<SendReconfirmBulkEmailJob> logger)
    : ICommandHandler<SendReconfirmBulkEmailCommand>
{
    private record EmailRecipient(
        Guid ParticipantId,
        Guid PublicId,
        string Email,
        string FirstName,
        string LastName,
        List<AdditionalDetail> AdditionalDetails,
        List<TicketSelection> Tickets,
        DateTimeOffset RegisteredAt,
        DateTimeOffset? ReconfirmEmailSentAt);

    public async ValueTask HandleAsync(SendReconfirmBulkEmailCommand command, CancellationToken cancellationToken)
    {
        var recipients = (await GetEmailRecipientsAsync(
                command.TicketedEventId,
                command.InitialDelayAfterRegistration,
                command.ReminderInterval,
                cancellationToken))
            .ToList();

        if (recipients.Count == 0)
        {
            logger.LogInformation("No recipients found for reconfirmation emails at this time based on attendee statuses.");
            return;
        }
        
        logger.LogInformation(
            "Sending reconfirmation emails to {RecipientCount} recipients(s)...",
            recipients.Count);

        var emailMessages = ComposeEmailsAsync(
            command.TicketedEventId,
            recipients,
            cancellationToken);

        await emailDispatcher.DispatchEmailsAsync(
            emailMessages,
            command.TeamId,
            command.TicketedEventId,
            command.CommandId,
            cancellationToken);
    }

    private async ValueTask<IEnumerable<EmailRecipient>> GetEmailRecipientsAsync(
        Guid ticketedEventId,
        TimeSpan initialDelayAfterRegistration,
        TimeSpan? reminderInterval,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return (await context.ParticipationView
                .AsNoTracking()
                .Where(p => p.TicketedEventId == ticketedEventId &&
                            p.AttendeeStatus == ParticipationAttendeeStatus.Registered)
                .Join(
                    context.Attendees,
                    p => p.AttendeeId,
                    a => a.Id,
                    (p, a) => new EmailRecipient(
                        p.ParticipantId,
                        p.PublicId,
                        p.Email,
                        a.FirstName,
                        a.LastName,
                        a.AdditionalDetails.ToList(),
                        a.Tickets.ToList(),
                        a.CreatedAt,
                        context.EmailLog
                            .Where(el => el.Recipient == p.Email && el.EmailType == WellKnownEmailType.Reconfirm)
                            .OrderByDescending(el => el.SentAt)
                            .Select(el => el.SentAt)
                            .FirstOrDefault()
                    ))
                .ToListAsync(cancellationToken))
            .Where(a => ShouldSendReconfirmEmail(
                a.Email,
                now,
                a.RegisteredAt,
                a.ReconfirmEmailSentAt,
                initialDelayAfterRegistration,
                reminderInterval));
    }

    private bool ShouldSendReconfirmEmail(
        string email,
        DateTimeOffset now,
        DateTimeOffset attendeeRegisteredAt,
        DateTimeOffset? latestReconfirmEmailSentAt,
        TimeSpan initialDelayAfterRegistration,
        TimeSpan? reminderInterval)
    {
        if (now < attendeeRegisteredAt + initialDelayAfterRegistration)
        {
            logger.LogInformation(
                "Skipping reconfirm email to {Email} - initial delay after registration not yet passed",
                email);
            return false;
        }
        
        if (latestReconfirmEmailSentAt is null)
            return true;

        // If no reminder interval is set, only send one reconfirm email
        if (reminderInterval is null)
        {
            logger.LogInformation(
                "Skipping reconfirm email to {Email} - reconfirm email already sent and no reminder interval is set",
                email);
            return false;
        }

        if (latestReconfirmEmailSentAt + reminderInterval > now)
        {
            logger.LogInformation(
                "Skipping reconfirm email to {Email} - reminder delay after previous email not yet passed",
                email);
            return false;
        }

        return true;
    }

    private async IAsyncEnumerable<EmailMessage> ComposeEmailsAsync(
        Guid ticketedEventId,
        IEnumerable<EmailRecipient> recipients,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var composer = new ReconfirmEmailComposer(signingService, emailTemplateService);

        var ticketedEvent = await context.TicketedEvents.GetWithoutTrackingAsync(
            ticketedEventId,
            cancellationToken);

        foreach (var recipient in recipients)
        {
            yield return await composer.ComposeMessageAsync(
                ticketedEvent.TeamId,
                ticketedEvent.Id,
                recipient.ParticipantId,
                recipient.PublicId,
                ticketedEvent.Name,
                ticketedEvent.Website,
                ticketedEvent.BaseUrl,
                ticketedEvent.TicketTypes.ToList(),
                recipient.Email,
                recipient.FirstName,
                recipient.LastName,
                recipient.AdditionalDetails,
                recipient.Tickets,
                cancellationToken);
        }
    }
}