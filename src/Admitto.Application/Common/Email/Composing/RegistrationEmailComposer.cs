using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email.Templating;
using Amolenk.Admitto.Domain.ValueObjects;
using Humanizer;

namespace Amolenk.Admitto.Application.Common.Email.Composing;

/// <summary>
/// Represents a composer for registration emails.
/// </summary>
public class RegistrationEmailComposer(
    IApplicationContext context,
    ISigningService signingService,
    IEmailTemplateService templateService)
    : EmailComposer(templateService)
{
    protected override async ValueTask<IEmailParameters> GetTemplateParametersAsync(
        Guid ticketedEventId,
        Guid entityId,
        Dictionary<string, string> additionalParameters,
        CancellationToken cancellationToken)
    {
        // For clarity.
        var attendeeId = entityId;

        var info = await context.Attendees
            .AsNoTracking()
            .Join(
                context.TicketedEvents,
                a => a.TicketedEventId,
                te => te.Id,
                (a, te) => new { Attendee = a, Event = te })
            .Join(
                context.TicketedEventAvailability,
                x => x.Event.Id,
                tea => tea.TicketedEventId,
                (x, tea) => new { x.Attendee, x.Event, Availability = tea })
            .Join(
                context.Participants,
                x => x.Attendee.ParticipantId,
                p => p.Id,
                (x, p) => new { x.Attendee, x.Event, x.Availability, Participant = p })
            .Where(x => x.Attendee.Id == attendeeId)
            .Select(x => new
            {
                x.Event.Name,
                x.Event.Website,
                x.Event.BaseUrl,
                x.Availability.TicketTypes,
                x.Attendee.Email,
                x.Attendee.FirstName,
                x.Attendee.LastName,
                x.Attendee.AdditionalDetails,
                x.Attendee.Tickets,
                x.Participant.PublicId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (info is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }

        var signature = await signingService.SignAsync(info.PublicId, ticketedEventId, cancellationToken);
        
        return new RegistrationEmailParameters(
            info.Name,
            info.Website,
            info.Email,
            EmailRecipientType.Attendee,
            info.FirstName,
            info.LastName,
            info.AdditionalDetails.Select(ad => new DetailEmailParameter(ad.Name, ad.Value)).ToList(),
            info.Tickets
                .Select(t => new TicketEmailParameter(
                    info.TicketTypes.First(tt => tt.Slug == t.TicketTypeSlug).Name,
                    t.Quantity))
                .ToList(),
            $"{info.BaseUrl}/tickets/qrcode/{info.PublicId}/{signature}",
            $"{info.BaseUrl}/tickets/reconfirm/{info.PublicId}/{signature}",
            $"{info.BaseUrl}/tickets/cancel/{info.PublicId}/{signature}");
    }

    protected override IEmailParameters GetTestTemplateParameters(
        string recipient,
        List<AdditionalDetail> additionalDetails,
        List<TicketSelection> tickets)
    {
        return new RegistrationEmailParameters(
            "Test Event",
            "www.example.com",
            recipient,
            EmailRecipientType.Other,
            "Alice",
            "Doe",
            additionalDetails.Select(ad => new DetailEmailParameter(ad.Name, ad.Value)).ToList(),
            tickets
                .Select(t => new TicketEmailParameter(t.TicketTypeSlug.Humanize(), t.Quantity))
                .ToList(),
            "https://www.example.com/tickets/qrcode/123/456",
            "https://www.example.com/tickets/reconfirm/123/456",
            "https://www.example.com/tickets/cancel/123/456");
    }
}