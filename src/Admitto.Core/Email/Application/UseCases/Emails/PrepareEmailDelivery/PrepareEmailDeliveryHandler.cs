using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.DeliverEmail;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;

internal sealed class PrepareEmailDeliveryHandler(
    IEmailWriteStore writeStore,
    [FromKeyedServices(EmailModule.Key)] IOutbox outbox)
    : ICommandHandler<PrepareEmailDeliveryCommand>, IWorkerOnly
{
    public async ValueTask HandleAsync(
        PrepareEmailDeliveryCommand command,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);
        var eventId = TicketedEventId.From(command.TicketedEventId);
        var recipient = EmailAddress.From(command.RecipientAddress);
        var existing = await writeStore.EmailLog.FirstOrDefaultAsync(
            log => log.TeamId == teamId
                && log.TicketedEventId == eventId
                && log.Recipient == recipient
                && log.IdempotencyKey == command.IdempotencyKey,
            cancellationToken);

        if (existing?.IsTerminal == true)
            return;

        if (existing is null)
        {
            writeStore.EmailLog.Add(EmailLog.Create(
                teamId,
                eventId,
                command.IdempotencyKey,
                recipient,
                command.EmailType,
                command.Subject,
                EmailLogStatus.Pending,
                sentAt: null,
                statusUpdatedAt: DateTimeOffset.UtcNow,
                registrationId: command.RegistrationId.HasValue
                    ? RegistrationId.From(command.RegistrationId.Value)
                    : null,
                registrationCycleId: command.RegistrationCycleId.HasValue
                    ? RegistrationCycleId.From(command.RegistrationCycleId.Value)
                    : null));
        }

        // A pending claim is intentionally re-enqueued as recovery work. The command
        // carries the complete rendered payload, so this path never needs templates
        // or SMTP configuration.
        outbox.Enqueue(new DeliverEmailCommand(
            command.TeamId,
            command.TicketedEventId,
            command.RecipientAddress,
            command.RecipientName,
            command.EmailType,
            command.IdempotencyKey,
            command.Subject,
            command.TextBody,
            command.HtmlBody));
    }
}
