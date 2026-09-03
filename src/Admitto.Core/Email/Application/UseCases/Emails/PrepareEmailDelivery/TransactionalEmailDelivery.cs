using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;

/// <summary>
/// Delivery identity and idempotency metadata kept outside transactional composition.
/// </summary>
internal sealed record TransactionalEmailDelivery(
    Guid TeamId,
    Guid TicketedEventId,
    string RecipientAddress,
    string RecipientName,
    string IdempotencyKey,
    Guid? RegistrationId = null,
    Guid? RegistrationCycleId = null);

internal static class TransactionalEmailDeliveryPreparation
{
    public static ValueTask PrepareAsync(
        ICommandHandler<PrepareEmailDeliveryCommand> handler,
        TransactionalEmailDelivery delivery,
        RenderedTransactionalEmail rendered,
        CancellationToken cancellationToken = default) =>
        handler.HandleAsync(
            new PrepareEmailDeliveryCommand(
                delivery.TeamId,
                delivery.TicketedEventId,
                delivery.RecipientAddress,
                delivery.RecipientName,
                rendered.EmailType,
                delivery.IdempotencyKey,
                rendered.Subject,
                rendered.TextBody,
                rendered.HtmlBody,
                delivery.RegistrationId,
                delivery.RegistrationCycleId),
            cancellationToken);
}
