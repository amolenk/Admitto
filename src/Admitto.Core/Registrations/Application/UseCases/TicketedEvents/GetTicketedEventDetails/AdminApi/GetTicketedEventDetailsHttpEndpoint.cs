using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventDetails.AdminApi;

public static class GetTicketedEventDetailsHttpEndpoint
{
    public static RouteGroupBuilder MapGetTicketedEventDetails(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetTicketedEventDetails)
            .WithName(nameof(GetTicketedEventDetails))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<Results<Ok<TicketedEventDetailsDto>, NotFound>> GetTicketedEventDetails(
        Guid teamId,
        Guid eventId,
        IQueryHandler<GetTicketedEventDetailsQuery, TicketedEventDetailsDto?> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetTicketedEventDetailsQuery(TicketedEventId.From(eventId), TeamId.From(teamId));

        var result = await handler.HandleAsync(query, cancellationToken);

        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
