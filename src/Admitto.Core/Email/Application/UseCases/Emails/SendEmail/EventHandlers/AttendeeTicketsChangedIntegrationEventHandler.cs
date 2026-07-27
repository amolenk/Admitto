using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

/// <summary>
/// Sends a new TicketConfirmation email when an attendee's tickets have changed.
/// </summary>
internal sealed class AttendeeTicketsChangedIntegrationEventHandler(
    IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto> eventContextQuery,
    ICommandHandler<SendEmailCommand> sendEmailHandler)
    : IIntegrationEventHandler<AttendeeTicketsChangedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        AttendeeTicketsChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var changedAtMs = integrationEvent.ChangedAt.ToUnixTimeMilliseconds();
        var idempotencyKey = $"tickets-changed:{integrationEvent.RegistrationId}:{changedAtMs}";

        var eventContext = await eventContextQuery.HandleAsync(
            new GetEventEmailRenderingContextQuery(
                TeamId.From(integrationEvent.TeamId),
                TicketedEventId.From(integrationEvent.TicketedEventId),
                RegistrationId.From(integrationEvent.RegistrationId)),
            cancellationToken);

        var fullName = $"{integrationEvent.FirstName} {integrationEvent.LastName}".Trim();
        var ticketTypeNames = integrationEvent.NewTickets.Select(t => t.Name).ToArray();

        var command = new SendEmailCommand(
            TeamId: integrationEvent.TeamId,
            TicketedEventId: integrationEvent.TicketedEventId,
            RecipientAddress: integrationEvent.RecipientEmail,
            RecipientName: fullName,
            EmailType: BuiltInEmailTemplateNames.TicketConfirmation,
            IdempotencyKey: idempotencyKey,
            Parameters: new
            {
                RecipientName = fullName,
                integrationEvent.FirstName,
                integrationEvent.LastName,
                eventContext.TeamName,
                eventContext.EventName,
                EventWebsite = eventContext.WebsiteUrl,
                eventContext.PublicEventLink,
                QRCodeLink = eventContext.QRCodeLink,
                eventContext.CancelLink,
                eventContext.EditRegistrationLink,
                TicketTypes = ticketTypeNames
            },
            RegistrationId: integrationEvent.RegistrationId);

        await sendEmailHandler.HandleAsync(command, cancellationToken);
    }
}
