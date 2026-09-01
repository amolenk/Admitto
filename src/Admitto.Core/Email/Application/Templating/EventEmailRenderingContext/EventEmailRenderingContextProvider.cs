using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTicketConfirmation;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;

internal interface IEventEmailRenderingContextProvider
{
    ValueTask<EventEmailRenderingScope> GetScopeAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        CancellationToken cancellationToken = default);

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
    public async ValueTask<EventEmailRenderingScope> GetScopeAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        CancellationToken cancellationToken = default)
    {
        var projection = await readStore.EventEmailContexts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.TeamId == teamId && c.TicketedEventId == ticketedEventId,
                cancellationToken);

        if (projection is null || !projection.HasRequiredRenderingContext)
            throw new EventEmailContextMissingException(teamId.Value, ticketedEventId.Value);

        var teamContext = await readStore.TeamEmailContexts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TeamId == teamId, cancellationToken);

        return new EventEmailRenderingScope(
            teamId,
            ticketedEventId,
            teamContext?.TeamName ?? "Admitto",
            teamContext?.AccentColor ?? AccentColor.From(AccentColor.Default),
            projection.EventName!,
            projection.WebsiteUrl!,
            BuildPublicEventLink(projection.PublicSlug!),
            projection.TimeZone ?? string.Empty,
            projection.ReconfirmOpensAt,
            projection.ReconfirmClosesAt,
            projection.ReconfirmMinEmailIntervalHours,
            projection.IsArchived);
    }

    public async ValueTask<EventEmailContextDto> GetContextAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        RegistrationId? registrationId,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetScopeAsync(teamId, ticketedEventId, cancellationToken);
        return scope.ToContext(registrationId);
    }

    private string BuildPublicEventLink(string publicSlug) =>
        $"{publicEventLinksOptions.Value.BaseUrl.TrimEnd('/')}/{publicSlug}";

}
