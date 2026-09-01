using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;

internal interface IEventEmailRenderingContextProvider
{
    ValueTask<EventEmailContextDto> GetContextAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        RegistrationId? registrationId,
        CancellationToken cancellationToken = default);
}

internal sealed class EventEmailRenderingContextProvider(
    IEmailReadStore readStore,
    IOptions<PublicEventLinksOptions> publicEventLinksOptions)
    : IEventEmailRenderingContextProvider
{
    public async ValueTask<EventEmailContextDto> GetContextAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        RegistrationId? registrationId,
        CancellationToken cancellationToken = default)
    {
        var projection = await readStore.EventEmailContexts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.TeamId == teamId && c.TicketedEventId == ticketedEventId,
                cancellationToken);

        var teamContext = await readStore.TeamEmailContexts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TeamId == teamId, cancellationToken);

        if (projection is null
            || !projection.HasRequiredRenderingContext
            || teamContext is null)
        {
            throw new EventEmailContextMissingException(teamId.Value, ticketedEventId.Value);
        }

        var publicEventLink = BuildPublicEventLink(projection.PublicSlug!);
        var links = RegistrationEmailLinks.From(publicEventLink, registrationId);

        return new EventEmailContextDto(
            teamId.Value,
            ticketedEventId.Value,
            teamContext.TeamName!,
            projection.EventName!,
            projection.WebsiteUrl!,
            links.PublicEventLink,
            links.RegisterLink,
            links.QRCodeLink,
            links.CancelLink,
            links.EditRegistrationLink,
            projection.TimeZone ?? string.Empty,
            projection.ReconfirmOpensAt,
            projection.ReconfirmClosesAt,
            projection.ReconfirmMinEmailIntervalHours,
            projection.IsArchived);
    }

    private string BuildPublicEventLink(string publicSlug) =>
        $"{publicEventLinksOptions.Value.BaseUrl.TrimEnd('/')}/{publicSlug}";
}
