using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePartnerTicketedEvent.PartnerApi;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetPartnerTicketedEventDetails.PartnerApi;

public static class GetPartnerTicketedEventDetailsHttpEndpoint
{
    public static RouteGroupBuilder MapGetPartnerTicketedEventDetails(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetPartnerTicketedEventDetails)
            .WithName(nameof(GetPartnerTicketedEventDetails));

        return group;
    }

    private static async ValueTask<Results<Ok<PartnerTicketedEventDetailsDto>, NotFound>> GetPartnerTicketedEventDetails(
        HttpContext httpContext,
        string eventSlug,
        PartnerTicketedEventResolver eventResolver,
        IQueryHandler<GetPartnerTicketedEventDetailsQuery, PartnerTicketedEventDetailsDto?> handler,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var eventId = await eventResolver.ResolveAsync(TeamId.From(teamId), eventSlug, cancellationToken);
        var query = new GetPartnerTicketedEventDetailsQuery(eventId, TeamId.From(teamId));

        var result = await handler.HandleAsync(query, cancellationToken);

        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
