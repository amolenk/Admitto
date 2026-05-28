using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail;

internal sealed class SendEmailHandler(
    IEmailWriteStore writeStore,
    IEffectiveEmailSettingsResolver settingsResolver,
    IEmailTemplateService templateService,
    IEmailRenderer renderer,
    IEmailSender emailSender) : ICommandHandler<SendEmailCommand>, IWorkerOnly
{
    public async ValueTask HandleAsync(SendEmailCommand command, CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);
        var ticketedEventId = TicketedEventId.From(command.TicketedEventId);
        var recipient = EmailAddress.From(command.RecipientAddress);
        RegistrationId? registrationId = command.RegistrationId.HasValue
            ? RegistrationId.From(command.RegistrationId.Value)
            : null;

        // Dedup: skip if already processed.
        var alreadySent = await writeStore.EmailLog
            .AnyAsync(l => l.IdempotencyKey == command.IdempotencyKey, cancellationToken);

        if (alreadySent)
            return;

        var now = DateTimeOffset.UtcNow;

        // Resolve effective settings.
        var settings = await settingsResolver.ResolveAsync(
            teamId,
            ticketedEventId,
            cancellationToken);

        if (settings is null || !settings.IsValid())
        {
            writeStore.EmailLog.Add(
                EmailLog.Create(
                    teamId: teamId,
                    ticketedEventId: ticketedEventId,
                    idempotencyKey: command.IdempotencyKey,
                    recipient: recipient,
                    emailType: command.EmailType,
                    subject: string.Empty,
                    provider: emailSender.Provider,
                    providerMessageId: null,
                    status: EmailLogStatus.Failed,
                    sentAt: null,
                    statusUpdatedAt: now,
                    lastError: "Email settings not configured or incomplete.",
                    registrationId: registrationId));
            return;
        }

        // Resolve + render template.
        RenderedEmail rendered;
        try
        {
            var template = await templateService.LoadAsync(
                command.EmailType,
                teamId,
                ticketedEventId,
                cancellationToken);
            rendered = renderer.Render(template, command.Parameters);
        }
        catch (EmailRenderException ex)
        {
            writeStore.EmailLog.Add(
                EmailLog.Create(
                    teamId: teamId,
                    ticketedEventId: ticketedEventId,
                    idempotencyKey: command.IdempotencyKey,
                    recipient: recipient,
                    emailType: command.EmailType,
                    subject: string.Empty,
                    provider: emailSender.Provider,
                    providerMessageId: null,
                    status: EmailLogStatus.Failed,
                    sentAt: null,
                    statusUpdatedAt: now,
                    lastError: ex.Message,
                    registrationId: registrationId));
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

            writeStore.EmailLog.Add(
                EmailLog.Create(
                    teamId: teamId,
                    ticketedEventId: ticketedEventId,
                    idempotencyKey: command.IdempotencyKey,
                    recipient: recipient,
                    emailType: command.EmailType,
                    subject: rendered.Subject,
                    provider: emailSender.Provider,
                    providerMessageId: providerMessageId,
                    status: EmailLogStatus.Sent,
                    sentAt: now,
                    statusUpdatedAt: now,
                    registrationId: registrationId));
        }
        catch (Exception ex)
        {
            writeStore.EmailLog.Add(
                EmailLog.Create(
                    teamId: teamId,
                    ticketedEventId: ticketedEventId,
                    idempotencyKey: command.IdempotencyKey,
                    recipient: recipient,
                    emailType: command.EmailType,
                    subject: rendered.Subject,
                    provider: emailSender.Provider,
                    providerMessageId: null,
                    status: EmailLogStatus.Failed,
                    sentAt: null,
                    statusUpdatedAt: DateTimeOffset.UtcNow,
                    lastError: ex.Message,
                    registrationId: registrationId));

            throw;
        }
    }
}
