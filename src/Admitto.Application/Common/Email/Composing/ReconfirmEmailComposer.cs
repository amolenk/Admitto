using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email.Templating;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Email.Composing;

/// <summary>
/// Represents a composer for reconfirmation emails.
/// </summary>
public class ReconfirmEmailComposer(ISigningService signingService, IEmailTemplateService templateService)
{
    private record TemplateParameters(
        string Recipient,
        string EventName,
        string EventWebsite,
        string FirstName,
        string LastName,
        List<AdditionalDetailParameter>? Details,
        List<TicketParameter>? Tickets,
        string ReconfirmLink,
        string EditLink,
        string CancelLink) : IEmailParameters;
    
    private record AdditionalDetailParameter(string Name, string Value);

    private record TicketParameter(string Slug, string Name, string[] SlotNames, int Quantity);
    
    public async ValueTask<EmailMessage> ComposeMessageAsync(
        Guid teamId,
        Guid ticketedEventId,
        Guid participantId,
        Guid publicId,
        string eventName,
        string eventWebsite,
        string eventBaseUrl,
        IList<TicketType> eventTicketTypes,
        string email,
        string firstName, 
        string lastName,
        IList<AdditionalDetail> details,
        IList<TicketSelection> tickets,
        CancellationToken cancellationToken = default)
    {
        var signature = await signingService.SignAsync(publicId, ticketedEventId, cancellationToken);
        
        var templateParameters = new TemplateParameters(
            email,
            eventName,
            eventWebsite,
            firstName,
            lastName,
            details
                .Select(ad => new AdditionalDetailParameter(ad.Name, ad.Value))
                .ToList(),
            tickets
                .Select(t =>
                {
                    var ticketType = eventTicketTypes.First(tt => tt.Slug == t.TicketTypeSlug);
                    return new TicketParameter(
                        ticketType.Slug,
                        ticketType.Name,
                        ticketType.SlotNames.ToArray(),
                        t.Quantity);
                })
                .ToList(),
            $"{eventBaseUrl}/tickets/reconfirm/{publicId}/{signature}",
            $"{eventBaseUrl}/tickets/edit/{publicId}/{signature}",
            $"{eventBaseUrl}/tickets/cancel/{publicId}/{signature}");
        
        return await templateService.RenderEmailMessageAsync(
            WellKnownEmailType.Reconfirm,
            teamId,
            ticketedEventId,
            templateParameters,
            participantId,
            cancellationToken);
    }
}