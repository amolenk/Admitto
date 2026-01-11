using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Data;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Composing;
using Amolenk.Admitto.Application.Common.Email.Templating;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.SendReconfirmEmail;

/// <summary>
/// Represents a handler that sends a single reconfirmation email to an attendee of a ticketed event.
/// </summary>
[RequiresCapability(HostCapability.Email)]
public class SendReconfirmEmailHandler(
    IApplicationContext context,
    ISigningService signingService,
    IEmailTemplateService emailTemplateService,
    IEmailDispatcher emailDispatcher)
    : ICommandHandler<SendReconfirmEmailCommand>
{
    public async ValueTask HandleAsync(SendReconfirmEmailCommand command, CancellationToken cancellationToken)
    {
        var emailMessage = await ComposeEmailAsync(
            command.TicketedEventId,
            command.AttendeeId,
            cancellationToken);

        await emailDispatcher.DispatchEmailAsync(
            emailMessage,
            command.TeamId,
            command.TicketedEventId,
            command.CommandId,
            cancellationToken);
    }

    private async ValueTask<EmailMessage> ComposeEmailAsync(
        Guid ticketedEventId,
        Guid attendeeId,
        CancellationToken cancellationToken = default)
    {
        var composer = new ReconfirmEmailComposer(signingService, emailTemplateService);

        var ticketedEvent = await context.TicketedEvents.GetWithoutTrackingAsync(ticketedEventId, cancellationToken);
        var attendee = await context.Attendees.GetWithoutTrackingAsync(attendeeId, ticketedEventId, cancellationToken);
        var participant = await context.Participants.GetWithoutTrackingAsync(attendee.ParticipantId, cancellationToken);
        
        return await composer.ComposeMessageAsync(
            ticketedEvent.TeamId,
            ticketedEvent.Id,
            participant.Id,
            participant.PublicId,
            ticketedEvent.Name,
            ticketedEvent.Website,
            ticketedEvent.BaseUrl,
            ticketedEvent.TicketTypes.ToList(),
            attendee.Email,
            attendee.FirstName,
            attendee.LastName,
            attendee.AdditionalDetails.ToList(),
            attendee.Tickets.ToList(),
            cancellationToken);
    }
}