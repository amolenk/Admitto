using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email.Templating;
using Amolenk.Admitto.Domain.ValueObjects;
using Humanizer;

namespace Amolenk.Admitto.Application.Common.Email.Composing;

/// <summary>
/// Represents a composer for registration emails.
/// </summary>
public class TicketEmailComposer(
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

        var item = await context.Attendees
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
            .Where(x => x.Event.Id == ticketedEventId && x.Attendee.Id == attendeeId)
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

        if (item is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }

        var signature = await signingService.SignAsync(item.PublicId, ticketedEventId, cancellationToken);
        
        return new TicketEmailParameters(
            item.Email,
            item.Name,
            item.Website,
            item.FirstName,
            item.LastName,
            item.AdditionalDetails.Select(ad => new DetailEmailParameter(ad.Name, ad.Value)).ToList(),
            item.Tickets
                .Select(t => new TicketEmailParameter(
                    item.TicketTypes.First(tt => tt.Slug == t.TicketTypeSlug).Name,
                    t.Quantity))
                .ToList(),
            $"{item.BaseUrl}/tickets/qrcode/{item.PublicId}/{signature}",
            $"{item.BaseUrl}/tickets/reconfirm/{item.PublicId}/{signature}",
            $"{item.BaseUrl}/tickets/cancel/{item.PublicId}/{signature}");
    }

    protected override IEmailParameters GetTestTemplateParameters(
        string recipient,
        List<AdditionalDetail> additionalDetails,
        List<TicketSelection> tickets)
    {
        return new TicketEmailParameters(
            recipient,
            "Test Event",
            "www.example.com",
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