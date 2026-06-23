using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.DeliverEmail;
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
    [FromKeyedServices(EmailModule.Key)] IOutbox outbox) : ICommandHandler<SendEmailCommand>, IWorkerOnly
{
    public async ValueTask HandleAsync(SendEmailCommand command, CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);
        var ticketedEventId = TicketedEventId.From(command.TicketedEventId);
        var recipient = EmailAddress.From(command.RecipientAddress);
        RegistrationId? registrationId = command.RegistrationId.HasValue
            ? RegistrationId.From(command.RegistrationId.Value)
            : null;

        // Dedup: skip terminal rows; pending rows can be retried by enqueueing delivery again.
        var existing = await writeStore.EmailLog
            .FirstOrDefaultAsync(
                l => l.TeamId == teamId &&
                     l.TicketedEventId == ticketedEventId &&
                     l.Recipient == recipient &&
                     l.IdempotencyKey == command.IdempotencyKey,
                cancellationToken);

        if (existing is not null && existing.IsTerminal)
            return;

        var now = DateTimeOffset.UtcNow;

        // Resolve effective settings.
        var settings = await settingsResolver.ResolveAsync(
            teamId,
            ticketedEventId,
            cancellationToken);

        if (settings is null || !settings.IsValid())
        {
            if (existing is null)
            {
                writeStore.EmailLog.Add(EmailLog.Create(
                    teamId: teamId,
                    ticketedEventId: ticketedEventId,
                    idempotencyKey: command.IdempotencyKey,
                    recipient: recipient,
                    emailType: command.EmailType,
                    subject: string.Empty,
                    status: EmailLogStatus.Failed,
                    sentAt: null,
                    statusUpdatedAt: now,
                    lastError: "Email settings not configured or incomplete.",
                    registrationId: registrationId));
            }
            else
            {
                existing.MarkFailed(string.Empty, "Email settings not configured or incomplete.", now);
            }
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
            if (existing is null)
            {
                writeStore.EmailLog.Add(EmailLog.Create(
                    teamId: teamId,
                    ticketedEventId: ticketedEventId,
                    idempotencyKey: command.IdempotencyKey,
                    recipient: recipient,
                    emailType: command.EmailType,
                    subject: string.Empty,
                    status: EmailLogStatus.Failed,
                    sentAt: null,
                    statusUpdatedAt: now,
                    lastError: ex.Message,
                    registrationId: registrationId));
            }
            else
            {
                existing.MarkFailed(string.Empty, ex.Message, now);
            }
            return;
        }

        if (existing is null)
        {
            writeStore.EmailLog.Add(EmailLog.Create(
                teamId: teamId,
                ticketedEventId: ticketedEventId,
                idempotencyKey: command.IdempotencyKey,
                recipient: recipient,
                emailType: command.EmailType,
                subject: rendered.Subject,
                status: EmailLogStatus.Pending,
                sentAt: null,
                statusUpdatedAt: now,
                registrationId: registrationId));
        }

        outbox.Enqueue(new DeliverEmailCommand(
            command.TeamId,
            command.TicketedEventId,
            command.RecipientAddress,
            command.RecipientName,
            command.EmailType,
            command.IdempotencyKey,
            rendered.Subject,
            rendered.TextBody,
            rendered.HtmlBody));
    }
}
