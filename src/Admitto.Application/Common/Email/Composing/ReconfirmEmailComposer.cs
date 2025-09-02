using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email.Templating;
using Amolenk.Admitto.Domain.ValueObjects;
using Humanizer;

namespace Amolenk.Admitto.Application.Common.Email.Composing;

/// <summary>
/// Represents a composer for reconfirmation emails.
/// </summary>
public class ReconfirmEmailComposer(
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
                x.Attendee.ParticipantId,
                x.Participant.PublicId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }

        var signature = await signingService.SignAsync(item.PublicId, ticketedEventId, cancellationToken);

        var parameters = new ReconfirmEmailParameters(
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
            $"{item.BaseUrl}/tickets/reconfirm/{item.PublicId}/{signature}",
            $"{item.BaseUrl}/tickets/cancel/{item.PublicId}/{signature}");

        return (parameters, item.ParticipantId);
    }

    protected override IEmailParameters GetTestTemplateParameters(
        string recipient,
        List<AdditionalDetail> additionalDetails,
        List<TicketSelection> tickets)
    {
        return new ReconfirmEmailParameters(
            recipient,
            "Test Event",
            "www.example.com",
            "Alice",
            "Doe",
            additionalDetails.Select(ad => new DetailEmailParameter(ad.Name, ad.Value)).ToList(),
            tickets
                .Select(t => new TicketEmailParameter(t.TicketTypeSlug.Humanize(), t.Quantity))
                .ToList(),
            "https://www.example.com/tickets/reconfirm/123/456",
            "https://www.example.com/tickets/cancel/123/456");
    }

    protected override async ValueTask<IEnumerable<Guid>> GetEntityIdsForBulkAsync(
        Guid ticketedEventId,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents.FindAsync([ticketedEventId], cancellationToken);
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        // TODO Check if we can reduce the number of columns fetched.

        // Query and join
        var items = await context.Attendees
            .AsNoTracking()
            .GroupJoin(
                context.EmailLog.Where(el => el.EmailType == WellKnownEmailType.Reconfirm),
                a => a.Email,
                el => el.Recipient,
                (a, el) => new { Attendee = a, EmailLogs = el })
            .Where(x =>
                x.Attendee.TicketedEventId == ticketedEventId &&
                x.Attendee.RegistrationStatus == RegistrationStatus.Registered)
            .Select(x => new
            {
                AttendeeId = x.Attendee.Id,
                AttendeeRegisteredAt = x.Attendee.CreatedAt,
                x.Attendee.TicketedEventId,
                SentReconfirmEmails = x.EmailLogs
            })
            .ToListAsync(cancellationToken);

        var reconfirmPolicy = ticketedEvent.ReconfirmPolicy;
        if (reconfirmPolicy is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.ReconfirmPolicyNotSet);
        }

        var now = DateTimeOffset.UtcNow;

        // Now filter in memory
        return items
            .Select(x => new
            {
                x.AttendeeId,
                x.AttendeeRegisteredAt,
                LatestReconfirmEmail = x.SentReconfirmEmails
                    .OrderByDescending(e => e.SentAt)
                    .FirstOrDefault()
            })
            .Where(x => reconfirmPolicy.NextSendAt(
                now,
                ticketedEvent.StartTime,
                x.AttendeeRegisteredAt,
                x.LatestReconfirmEmail?.SentAt) <= now)
            .Select(x => x.AttendeeId)
            .ToList();
    }
}