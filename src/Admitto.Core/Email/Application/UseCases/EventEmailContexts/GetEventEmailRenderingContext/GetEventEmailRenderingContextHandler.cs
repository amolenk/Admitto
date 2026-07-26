using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;

internal sealed class GetEventEmailRenderingContextHandler(
    IEmailReadStore readStore,
    IOptions<PublicEventLinksOptions> publicEventLinksOptions)
    : IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto>
{
    public async ValueTask<EventEmailContextDto> HandleAsync(
        GetEventEmailRenderingContextQuery query,
        CancellationToken cancellationToken)
    {
        var projection = await readStore.EventEmailContexts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.TeamId == query.TeamId && c.TicketedEventId == query.TicketedEventId,
                cancellationToken);

        var teamContext = await readStore.TeamEmailContexts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TeamId == query.TeamId, cancellationToken);

        if (projection is null
            || !projection.HasRequiredRenderingContext
            || teamContext is null)
        {
            throw new EventEmailContextMissingException(query.TeamId.Value, query.TicketedEventId.Value);
        }

        var publicEventLink = BuildPublicEventLink(projection.PublicSlug!);
        var registrationSuffix = query.RegistrationId?.Value.ToString() ?? string.Empty;
        var hasRegistration = !string.IsNullOrWhiteSpace(registrationSuffix);

        return new EventEmailContextDto(
            query.TeamId.Value,
            query.TicketedEventId.Value,
            teamContext.TeamName!,
            projection.EventName!,
            projection.WebsiteUrl!,
            publicEventLink,
            $"{publicEventLink}/register",
            hasRegistration ? $"{publicEventLink}/qr-code/{registrationSuffix}" : publicEventLink,
            hasRegistration ? $"{publicEventLink}/cancel/{registrationSuffix}" : publicEventLink,
            hasRegistration ? $"{publicEventLink}/edit/{registrationSuffix}" : publicEventLink,
            projection.TimeZone ?? string.Empty,
            projection.ReconfirmOpensAt,
            projection.ReconfirmClosesAt,
            projection.ReconfirmCadenceHours,
            projection.ReconfirmMinEmailIntervalHours,
            projection.IsArchived);
    }

    private string BuildPublicEventLink(string publicSlug) =>
        $"{publicEventLinksOptions.Value.BaseUrl.TrimEnd('/')}/{publicSlug}";
}
