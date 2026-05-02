using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Application.UseCases.SendEmail;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.SendEmail.EventHandlers;

/// <summary>
/// Handles <see cref="RegistrationCancelledIntegrationEvent"/> by dispatching a
/// <see cref="SendEmailCommand"/> with the appropriate cancellation template.
/// </summary>
/// <remarks>
/// Template routing: AttendeeRequest → cancellation; VisaLetterDenied → visa-letter-denied.
/// TicketTypesRemoved is a no-op (handled by a future change).
/// Idempotency key: <c>registration-cancelled:{registrationId}</c>.
/// </remarks>
internal sealed class RegistrationCancelledIntegrationEventHandler(
    IEmailWriteStore writeStore,
    IRegistrationsFacade registrationsFacade,
    IMediator mediator)
    : IIntegrationEventHandler<RegistrationCancelledIntegrationEvent>
{
    public async ValueTask HandleAsync(
        RegistrationCancelledIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var emailType = ResolveEmailType(integrationEvent.Reason);
        if (emailType is null)
            return;

        var idempotencyKey = $"registration-cancelled:{integrationEvent.RegistrationId}";

        var alreadyHandled = await writeStore.EmailLog
            .AnyAsync(l => l.IdempotencyKey == idempotencyKey, cancellationToken);

        if (alreadyHandled)
            return;

        var eventContext = await registrationsFacade.GetTicketedEventEmailContextAsync(
            integrationEvent.TicketedEventId,
            integrationEvent.RegistrationId,
            cancellationToken);

        var firstName = eventContext.FirstName ?? string.Empty;
        var lastName = eventContext.LastName ?? string.Empty;

        var command = new SendEmailCommand(
            TeamId: TeamId.From(integrationEvent.TeamId),
            TicketedEventId: TicketedEventId.From(integrationEvent.TicketedEventId),
            RegistrationId: integrationEvent.RegistrationId,
            RecipientAddress: integrationEvent.RecipientEmail,
            RecipientName: $"{firstName} {lastName}".Trim(),
            EmailType: emailType,
            IdempotencyKey: idempotencyKey,
            Parameters: new
            {
                FirstName = firstName,
                LastName = lastName,
                EventName = eventContext.Name,
                EventWebsite = eventContext.WebsiteUrl,
                QRCodeLink = eventContext.QRCodeLink
            });

        await mediator.SendAsync(command, cancellationToken);
    }

    private static string? ResolveEmailType(string reason) => reason switch
    {
        "AttendeeRequest" => EmailTemplateType.Cancellation,
        "VisaLetterDenied" => EmailTemplateType.VisaLetterDenied,
        _ => null
    };
}
