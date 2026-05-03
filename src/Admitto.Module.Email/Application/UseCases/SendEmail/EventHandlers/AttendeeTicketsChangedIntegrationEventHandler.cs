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
/// Handles <see cref="AttendeeTicketsChangedIntegrationEvent"/> by dispatching a
/// <see cref="SendEmailCommand"/> to send a ticket-change confirmation email.
/// </summary>
/// <remarks>
/// Idempotency key: <c>tickets-changed:{registrationId}:{changedAt-unix-ms}</c>.
/// The <c>ticket_types</c> parameter lists the new ticket type names.
/// </remarks>
internal sealed class AttendeeTicketsChangedIntegrationEventHandler(
    IEmailWriteStore writeStore,
    IRegistrationsFacade registrationsFacade,
    IMediator mediator)
    : IIntegrationEventHandler<AttendeeTicketsChangedIntegrationEvent>
{
    public async ValueTask HandleAsync(
        AttendeeTicketsChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var changedAtMs = integrationEvent.ChangedAt.ToUnixTimeMilliseconds();
        var idempotencyKey = $"tickets-changed:{integrationEvent.RegistrationId}:{changedAtMs}";

        var alreadyHandled = await writeStore.EmailLog
            .AnyAsync(l => l.IdempotencyKey == idempotencyKey, cancellationToken);

        if (alreadyHandled)
            return;

        var eventContext = await registrationsFacade.GetTicketedEventEmailContextAsync(
            integrationEvent.TicketedEventId,
            integrationEvent.RegistrationId,
            cancellationToken);

        var fullName = $"{integrationEvent.FirstName} {integrationEvent.LastName}".Trim();
        var ticketTypeNames = integrationEvent.NewTickets.Select(t => t.Name).ToArray();

        var command = new SendEmailCommand(
            TeamId: TeamId.From(integrationEvent.TeamId),
            TicketedEventId: TicketedEventId.From(integrationEvent.TicketedEventId),
            RecipientAddress: integrationEvent.RecipientEmail,
            RecipientName: fullName,
            EmailType: EmailTemplateType.Ticket,
            IdempotencyKey: idempotencyKey,
            Parameters: new
            {
                RecipientName = fullName,
                integrationEvent.FirstName,
                integrationEvent.LastName,
                EventName = eventContext.Name,
                EventWebsite = eventContext.WebsiteUrl,
                QRCodeLink = eventContext.QRCodeLink,
                TicketTypes = ticketTypeNames
            },
            RegistrationId: integrationEvent.RegistrationId);

        await mediator.SendAsync(command, cancellationToken);
    }
}
