using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email.Templating;
using Amolenk.Admitto.Domain.ValueObjects;
using Humanizer;

namespace Amolenk.Admitto.Application.Common.Email.Composing;

/// <summary>
/// Represents a composer for cancel confirmation emails.
/// </summary>
public class CanceledEmailComposer(
    IApplicationContext context,
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

        var item = await context.ParticipationView
            .AsNoTracking()
            .Join(
                context.TicketedEvents,
                p => p.TicketedEventId,
                te => te.Id,
                (p, te) => new { Participation = p, Event = te })
            .Where(x => x.Event.Id == ticketedEventId && x.Participation.AttendeeId == attendeeId)
            .Select(x => new
            {
                x.Event.Name,
                x.Event.Website,
                x.Event.BaseUrl,
                x.Participation.Email,
                x.Participation.FirstName,
                x.Participation.LastName,
                x.Participation.ParticipantId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Participant.NotFound);
        }

        var parameters = new CanceledEmailParameters(
            item.Email,
            item.Name,
            item.Website,
            item.FirstName,
            item.LastName,
            $"{item.BaseUrl}/tickets");

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
        
        return new CanceledEmailParameters(
            recipient,
            ticketedEvent.Name,
            ticketedEvent.Website,
            "Alice",
            "Doe",
            $"{ticketedEvent.BaseUrl}/tickets");
    }
}