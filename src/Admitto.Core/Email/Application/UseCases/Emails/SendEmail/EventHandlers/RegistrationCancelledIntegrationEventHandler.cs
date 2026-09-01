using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTransactionalEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Handles <see cref="RegistrationCancelledIntegrationEvent"/> by mapping its
/// reason to a typed cancellation intent and dispatching the composer.
/// </summary>
/// <remarks>
/// Template routing: AttendeeRequest → cancellation; VisaLetterDenied → visa-letter-denied.
/// TicketTypesRemoved is a no-op.
/// Idempotency key: <c>registration-cancelled:{integrationEventId}</c>.
/// </remarks>
internal sealed class RegistrationCancelledIntegrationEventHandler(
    ITransactionalEmailComposer composer,
    ICommandHandler<PrepareEmailDeliveryCommand> prepareDeliveryHandler)
    : IIntegrationEventHandler<RegistrationCancelledIntegrationEvent>
{
    public async ValueTask HandleAsync(
        RegistrationCancelledIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"registration-cancelled:{integrationEvent.IntegrationEventId:N}";
        var dispatch = ResolveDispatch(integrationEvent, idempotencyKey);
        if (dispatch is null)
            return;

        var rendered = await composer.ComposeAsync(dispatch.Intent, cancellationToken);
        await TransactionalEmailDeliveryPreparation.PrepareAsync(
            prepareDeliveryHandler,
            dispatch.Delivery,
            rendered,
            cancellationToken);
    }

    private static CancellationEmailDispatch? ResolveDispatch(
        RegistrationCancelledIntegrationEvent integrationEvent,
        string idempotencyKey)
    {
        var teamId = TeamId.From(integrationEvent.TeamId);
        var eventId = TicketedEventId.From(integrationEvent.TicketedEventId);
        var registrationId = RegistrationId.From(integrationEvent.RegistrationId);
        var recipientName = $"{integrationEvent.FirstName} {integrationEvent.LastName}".Trim();
        return integrationEvent.Reason switch
        {
            "AttendeeRequest" => new CancellationEmailDispatch(
                new AttendeeRequestCancellationIntent(teamId, eventId, integrationEvent.FirstName, registrationId),
                new TransactionalEmailDelivery(
                    integrationEvent.TeamId, integrationEvent.TicketedEventId,
                    integrationEvent.RecipientEmail, recipientName, idempotencyKey, integrationEvent.RegistrationId)),
            "VisaLetterDenied" => new CancellationEmailDispatch(
                new VisaLetterDeniedCancellationIntent(teamId, eventId, integrationEvent.FirstName, registrationId),
                new TransactionalEmailDelivery(
                    integrationEvent.TeamId, integrationEvent.TicketedEventId,
                    integrationEvent.RecipientEmail, recipientName, idempotencyKey, integrationEvent.RegistrationId)),
            "ReconfirmAutoCancel" => new CancellationEmailDispatch(
                new ReconfirmAutoCancellationIntent(teamId, eventId, integrationEvent.FirstName, registrationId),
                new TransactionalEmailDelivery(
                    integrationEvent.TeamId, integrationEvent.TicketedEventId,
                    integrationEvent.RecipientEmail, recipientName, idempotencyKey, integrationEvent.RegistrationId)),
            _ => null
        };
    }

    private sealed record CancellationEmailDispatch(
        RegistrationCancellationIntent Intent,
        TransactionalEmailDelivery Delivery);
}
