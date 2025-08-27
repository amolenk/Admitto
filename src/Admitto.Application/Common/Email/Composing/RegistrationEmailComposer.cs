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
        Guid entityId,
        Dictionary<string, string> additionalParameters,
        CancellationToken cancellationToken)
    {
        // For clarity.
        var registrationId = entityId;
        
        var info = await context.AttendeeRegistrations
            .AsNoTracking()
            .Join(
                context.TicketedEvents,
                r => r.TicketedEventId,
                e => e.Id,
                (r, e) => new { Registration = r, Event = e })
            .Where(joined => joined.Registration.Id == registrationId)
            .Select(joined => new
            {
                joined.Event.Name,
                joined.Event.Website,
                joined.Event.TicketTypes,
                joined.Event.BaseUrl,
                joined.Registration.Email,
                joined.Registration.FirstName,
                joined.Registration.LastName,
                joined.Registration.AdditionalDetails,
                joined.Registration.Tickets
            })
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (info is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound(registrationId));
        }

        return new RegistrationEmailParameters(
            info.Name,
            info.Website,
            info.Email,
            info.FirstName,
            info.LastName,
            info.AdditionalDetails.Select(ad => new DetailEmailParameter(ad.Name, ad.Value)).ToList(),
            info.Tickets
                .Select(t => new TicketEmailParameter(
                    info.TicketTypes.First(tt => tt.Slug == t.TicketTypeSlug).Name,
                    t.Quantity))
                .ToList(),
            $"{info.BaseUrl}/tickets/qrcode/{registrationId}/{signingService.Sign(registrationId)}",
            $"{info.BaseUrl}/tickets/reconfirm/{registrationId}/{signingService.Sign(registrationId)}",
            $"{info.BaseUrl}/tickets/cancel/{registrationId}/{signingService.Sign(registrationId)}");
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
            "Alice",
            "Doe",
            additionalDetails .Select(ad => new DetailEmailParameter(ad.Name, ad.Value)).ToList(),
            tickets
                .Select(t => new TicketEmailParameter(t.TicketTypeSlug.Humanize(), t.Quantity))
                .ToList(),
            "https://www.example.com/tickets/qrcode/123/456",
            "https://www.example.com/tickets/reconfirm/123/456",
            "https://www.example.com/tickets/cancel/123/456");
    }
}