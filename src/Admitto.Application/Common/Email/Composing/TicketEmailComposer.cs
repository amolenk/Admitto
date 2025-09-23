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
    protected override async ValueTask<(IEmailParameters Parameters, Guid? ParticipantId)> GetTemplateParametersAsync(
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
                context.Participants,
                x => x.Attendee.ParticipantId,
                p => p.Id,
                (x, p) => new { x.Attendee, x.Event, Participant = p })
            .Where(x => x.Event.Id == ticketedEventId && x.Attendee.Id == attendeeId)
            .Select(x => new
            {
                x.Event.Name,
                x.Event.Website,
                x.Event.BaseUrl,
                x.Event.TicketTypes,
                x.Attendee.Email,
                x.Attendee.FirstName,
                x.Attendee.LastName,
                x.Attendee.AdditionalDetails,
                x.Attendee.Tickets,
                x.Attendee.ParticipantId,
                x.Participant.PublicId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }

        var signature = await signingService.SignAsync(item.PublicId, ticketedEventId, cancellationToken);

        var parameters = new TicketEmailParameters(
            item.Email,
            item.Name,
            item.Website,
            item.FirstName,
            item.LastName,
            item.AdditionalDetails.Select(ad => new DetailEmailParameter(ad.Name, ad.Value)).ToList(),
            item.Tickets
                .Select(t =>
                {
                    var ticketType = item.TicketTypes.First(tt => tt.Slug == t.TicketTypeSlug);
                    return new TicketEmailParameter(
                        ticketType.Slug,
                        ticketType.Name,
                        ticketType.SlotNames.ToArray(),
                        t.Quantity);
                })
                .ToList(),
            $"{item.BaseUrl}/tickets/qrcode/{item.PublicId}/{signature}",
            $"{item.BaseUrl}/tickets/edit/{item.PublicId}/{signature}",
            $"{item.BaseUrl}/tickets/cancel/{item.PublicId}/{signature}");

        return (parameters, item.ParticipantId);
    }

    protected override async ValueTask<IEmailParameters> GetTestTemplateParametersAsync(
        Guid ticketedEventId,
        string recipient,
        List<AdditionalDetail> additionalDetails,
        List<TicketSelection> tickets, 
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents
            .AsNoTracking()
            .Where(x => x.Id == ticketedEventId)
            .Select(x => new
            {
                x.Name,
                x.Website,
                x.BaseUrl,
                x.TicketTypes
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }
        
        return new TicketEmailParameters(
            recipient,
            ticketedEvent.Name,
            ticketedEvent.Website,
            "Alice",
            "Doe",
            additionalDetails.Select(ad => new DetailEmailParameter(ad.Name, ad.Value)).ToList(),
            tickets
                .Select(t =>
                {
                    var ticketType = ticketedEvent.TicketTypes.First(tt => tt.Slug == t.TicketTypeSlug);
                    return new TicketEmailParameter(
                        ticketType.Slug,
                        ticketType.Name,
                        ticketType.SlotNames.ToArray(),
                        t.Quantity);
                })
                .ToList(),
            $"{ticketedEvent.BaseUrl}/tickets/qrcode/123/456",
            $"{ticketedEvent.BaseUrl}/tickets/edit/123/456",
            $"{ticketedEvent.BaseUrl}/tickets/cancel/123/456");
    }
}