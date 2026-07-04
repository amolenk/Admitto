using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePartnerTicketedEvent.PartnerApi;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.GetPublicTicketTypes.PartnerApi;

public static class GetPublicTicketTypesHttpEndpoint
{
    public static RouteGroupBuilder MapGetPublicTicketTypes(this RouteGroupBuilder group)
    {
        group.MapGet("/ticket-types", GetPublicTicketTypes)
            .WithName(nameof(GetPublicTicketTypes));

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<PublicTicketTypeDto>>> GetPublicTicketTypes(
        HttpContext httpContext,
        string eventSlug,
        PartnerTicketedEventResolver eventResolver,
        IQueryHandler<GetPublicTicketTypesQuery, IReadOnlyList<PublicTicketTypeDto>> handler,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var eventId = await eventResolver.ResolveAsync(TeamId.From(teamId), eventSlug, cancellationToken);
        var query = new GetPublicTicketTypesQuery(eventId, TeamId.From(teamId));

        var result = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
