using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeRegistrationCancellation;
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
    IRegistrationCancellationEmailComposer composer)
    : IIntegrationEventHandler<RegistrationCancelledIntegrationEvent>
{
    public async ValueTask HandleAsync(
        RegistrationCancelledIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var intent = ResolveIntent(integrationEvent.Reason, integrationEvent.FirstName);
        if (intent is null)
            return;

        var idempotencyKey = $"registration-cancelled:{integrationEvent.IntegrationEventId:N}";

        await composer.ComposeAsync(
            TeamId.From(integrationEvent.TeamId),
            TicketedEventId.From(integrationEvent.TicketedEventId),
            intent,
            new RegistrationCancellationDelivery(
                integrationEvent.RecipientEmail,
                $"{integrationEvent.FirstName} {integrationEvent.LastName}".Trim(),
                idempotencyKey,
                RegistrationId.From(integrationEvent.RegistrationId)),
            cancellationToken);
    }

    private static RegistrationCancellationIntent? ResolveIntent(string reason, string firstName) => reason switch
    {
        "AttendeeRequest" => new AttendeeRequestCancellationIntent(firstName),
        "VisaLetterDenied" => new VisaLetterDeniedCancellationIntent(firstName),
        "ReconfirmAutoCancel" => new ReconfirmAutoCancellationIntent(firstName),
        _ => null
    };
}
