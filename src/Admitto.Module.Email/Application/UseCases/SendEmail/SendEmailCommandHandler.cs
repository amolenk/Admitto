using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Application.Sending;
using Amolenk.Admitto.Module.Email.Application.Settings;
using Amolenk.Admitto.Module.Email.Application.Templating;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.SendEmail;

[RequiresCapability(HostCapability.Email)]
internal sealed class SendEmailCommandHandler(
    IEmailWriteStore writeStore,
    IEffectiveEmailSettingsResolver settingsResolver,
    IEmailTemplateService templateService,
    IEmailRenderer renderer,
    IEmailSender emailSender) : ICommandHandler<SendEmailCommand>
{
    public async ValueTask HandleAsync(SendEmailCommand command, CancellationToken cancellationToken)
    {
        // Dedup: skip if already processed.
        var alreadySent = await writeStore.EmailLog
            .AnyAsync(l => l.IdempotencyKey == command.IdempotencyKey, cancellationToken);

        if (alreadySent)
            return;

        var now = DateTimeOffset.UtcNow;

        // Resolve effective settings.
        var settings = await settingsResolver.ResolveAsync(
            command.TeamId, command.TicketedEventId, cancellationToken);

        if (settings is null || !settings.IsValid())
        {
            writeStore.EmailLog.Add(EmailLog.Create(
                teamId: command.TeamId.Value,
                ticketedEventId: command.TicketedEventId.Value,
                idempotencyKey: command.IdempotencyKey,
                recipient: command.RecipientAddress,
                emailType: command.EmailType,
                subject: string.Empty,
                provider: emailSender.Provider,
                providerMessageId: null,
                status: EmailLogStatus.Failed,
                sentAt: null,
                statusUpdatedAt: now,
                lastError: "Email settings not configured or incomplete."));
            return;
        }

        // Resolve + render template.
        RenderedEmail rendered;
        try
        {
            var template = await templateService.LoadAsync(
                command.EmailType, command.TeamId, command.TicketedEventId, cancellationToken);
            rendered = renderer.Render(template, command.Parameters);
        }
        catch (EmailRenderException ex)
        {
            writeStore.EmailLog.Add(EmailLog.Create(
                teamId: command.TeamId.Value,
                ticketedEventId: command.TicketedEventId.Value,
                idempotencyKey: command.IdempotencyKey,
                recipient: command.RecipientAddress,
                emailType: command.EmailType,
                subject: string.Empty,
                provider: emailSender.Provider,
                providerMessageId: null,
                status: EmailLogStatus.Failed,
                sentAt: null,
                statusUpdatedAt: now,
                lastError: ex.Message));
            return;
        }

        // Send.
        try
        {
            var message = new EmailMessage(
                RecipientAddress: command.RecipientAddress,
                RecipientName: command.RecipientName,
                Subject: rendered.Subject,
                TextBody: rendered.TextBody,
                HtmlBody: rendered.HtmlBody);

            var providerMessageId = await emailSender.SendAsync(settings, message, cancellationToken);

            writeStore.EmailLog.Add(EmailLog.Create(
                teamId: command.TeamId.Value,
                ticketedEventId: command.TicketedEventId.Value,
                idempotencyKey: command.IdempotencyKey,
                recipient: command.RecipientAddress,
                emailType: command.EmailType,
                subject: rendered.Subject,
                provider: emailSender.Provider,
                providerMessageId: providerMessageId,
                status: EmailLogStatus.Sent,
                sentAt: now,
                statusUpdatedAt: now));
        }
        catch (Exception ex)
        {
            writeStore.EmailLog.Add(EmailLog.Create(
                teamId: command.TeamId.Value,
                ticketedEventId: command.TicketedEventId.Value,
                idempotencyKey: command.IdempotencyKey,
                recipient: command.RecipientAddress,
                emailType: command.EmailType,
                subject: rendered.Subject,
                provider: emailSender.Provider,
                providerMessageId: null,
                status: EmailLogStatus.Failed,
                sentAt: null,
                statusUpdatedAt: DateTimeOffset.UtcNow,
                lastError: ex.Message));

            throw;
        }
    }
}
