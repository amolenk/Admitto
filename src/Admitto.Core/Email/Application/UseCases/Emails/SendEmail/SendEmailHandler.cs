using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail;

internal sealed class SendEmailHandler(
    IEmailWriteStore writeStore,
    IEmailPreparationService preparationService,
    ICommandHandler<PrepareEmailDeliveryCommand> prepareDeliveryHandler) : ICommandHandler<SendEmailCommand>, IWorkerOnly
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

        RenderedEmail rendered;
        try
        {
            rendered = await preparationService.PrepareAsync(
                command.EmailType,
                teamId,
                ticketedEventId,
                command.Parameters,
                cancellationToken);
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

        await prepareDeliveryHandler.HandleAsync(
            new PrepareEmailDeliveryCommand(
                command.TeamId,
                command.TicketedEventId,
                command.RecipientAddress,
                command.RecipientName,
                command.EmailType,
                command.IdempotencyKey,
                rendered.Subject,
                rendered.TextBody,
                rendered.HtmlBody,
                command.RegistrationId),
            cancellationToken);
    }
}
