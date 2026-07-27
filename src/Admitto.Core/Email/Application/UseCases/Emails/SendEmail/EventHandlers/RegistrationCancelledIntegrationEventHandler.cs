using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

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
    IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto> eventContextQuery,
    ICommandHandler<SendEmailCommand> sendEmailHandler)
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

        var eventContext = await eventContextQuery.HandleAsync(
            new GetEventEmailRenderingContextQuery(
                TeamId.From(integrationEvent.TeamId),
                TicketedEventId.From(integrationEvent.TicketedEventId),
                RegistrationId.From(integrationEvent.RegistrationId)),
            cancellationToken);

        var firstName = integrationEvent.FirstName;
        var lastName = integrationEvent.LastName;

        var command = new SendEmailCommand(
            TeamId: integrationEvent.TeamId,
            TicketedEventId: integrationEvent.TicketedEventId,
            RegistrationId: integrationEvent.RegistrationId,
            RecipientAddress: integrationEvent.RecipientEmail,
            RecipientName: $"{firstName} {lastName}".Trim(),
            EmailType: emailType,
            IdempotencyKey: idempotencyKey,
            Parameters: new
            {
                FirstName = firstName,
                LastName = lastName,
                eventContext.TeamName,
                eventContext.EventName,
                EventWebsite = eventContext.WebsiteUrl,
                RegisterLink = eventContext.RegisterLink,
                eventContext.QRCodeLink,
            });

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }

    private static string? ResolveEmailType(string reason) => reason switch
    {
        "AttendeeRequest" => BuiltInEmailTemplateNames.Cancellation,
        "VisaLetterDenied" => BuiltInEmailTemplateNames.VisaLetterDenied,
        "ReconfirmAutoCancel" => BuiltInEmailTemplateNames.ReconfirmCancelled,
        _ => null
    };
}
