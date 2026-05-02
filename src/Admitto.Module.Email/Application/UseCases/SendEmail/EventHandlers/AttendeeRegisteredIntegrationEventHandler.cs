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
/// Handles <see cref="AttendeeRegisteredIntegrationEvent"/> by dispatching a
/// <see cref="SendEmailCommand"/> to send a registration confirmation email.
/// </summary>
/// <remarks>
/// No capability gate — this handler runs in any host that processes the Registrations queue.
/// The actual send is gated on <see cref="HostCapability.Email"/> inside <see cref="SendEmailCommandHandler"/>.
/// Idempotency key: <c>attendee-registered:{registrationId}</c>.
/// Event name, website URL, and pre-signed links are all returned by the Registrations facade
/// so signing infra stays inside the Registrations module.
/// </remarks>
internal sealed class AttendeeRegisteredIntegrationEventHandler(
    IEmailWriteStore writeStore,
    IRegistrationsFacade registrationsFacade,
    IMediator mediator)
    : IIntegrationEventHandler<AttendeeRegisteredIntegrationEvent>
{
    public async ValueTask HandleAsync(
        AttendeeRegisteredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"attendee-registered:{integrationEvent.RegistrationId}";

        var alreadyHandled = await writeStore.EmailLog
            .AnyAsync(l => l.IdempotencyKey == idempotencyKey, cancellationToken);

        if (alreadyHandled)
            return;

        var eventContext = await registrationsFacade.GetTicketedEventEmailContextAsync(
            integrationEvent.TicketedEventId,
            integrationEvent.RegistrationId,
            cancellationToken);

        var fullName = $"{integrationEvent.FirstName} {integrationEvent.LastName}".Trim();

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
                QRCodeLink = eventContext.QRCodeLink
            },
            RegistrationId: integrationEvent.RegistrationId);

        await mediator.SendAsync(command, cancellationToken);
    }
}
