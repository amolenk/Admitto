using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.DeliverEmail;

internal sealed class DeliverEmailHandler(
    IEmailWriteStore writeStore,
    IEffectiveEmailSettingsResolver settingsResolver,
    IEmailSender emailSender,
    [FromKeyedServices(EmailModule.Key)] IOutbox outbox,
    IOptionsMonitor<EmailDeliveryOptions> options)
    : ICommandHandler<DeliverEmailCommand>, IWorkerOnly
{
    public async ValueTask HandleAsync(DeliverEmailCommand command, CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);
        var ticketedEventId = TicketedEventId.From(command.TicketedEventId);
        var recipient = EmailAddress.From(command.RecipientAddress);
        var now = DateTimeOffset.UtcNow;

        var log = await FindLogAsync(teamId, ticketedEventId, recipient, command.IdempotencyKey, cancellationToken);
        if (log is null || log.IsTerminal)
            return;

        var settings = await settingsResolver.ResolveAsync(teamId, ticketedEventId, cancellationToken);
        if (settings is null || !settings.IsValid())
        {
            log.MarkFailed(command.Subject, "Email settings not configured or incomplete.", now);
            return;
        }

        var message = new EmailMessage(
            RecipientAddress: command.RecipientAddress,
            RecipientName: command.RecipientName,
            Subject: command.Subject,
            TextBody: command.TextBody,
            HtmlBody: command.HtmlBody);

        Exception? lastException = null;
        for (var attempt = 0; attempt <= options.CurrentValue.InlineRetryCount; attempt++)
        {
            if (attempt > 0 && options.CurrentValue.InlineRetryDelay > TimeSpan.Zero)
                await Task.Delay(options.CurrentValue.InlineRetryDelay, cancellationToken);

            try
            {
                await emailSender.SendAsync(settings, message, cancellationToken);
                log.MarkSent(command.Subject, DateTimeOffset.UtcNow);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        var failedAt = DateTimeOffset.UtcNow;
        var error = lastException?.Message ?? "SMTP delivery failed.";
        if (log.DeliveryAttemptCount + 1 >= options.CurrentValue.MaxDeliveryAttempts)
        {
            log.MarkFailed(command.Subject, error, failedAt);
            return;
        }

        log.MarkRetryableFailure(command.Subject, error, failedAt);
        outbox.Enqueue(command);
    }

    private async Task<EmailLog?> FindLogAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        EmailAddress recipient,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await writeStore.EmailLog.FirstOrDefaultAsync(
            l => l.TeamId == teamId &&
                 l.TicketedEventId == ticketedEventId &&
                 l.Recipient == recipient &&
                 l.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }
}
