using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.GetTicketTypes.AdminApi;

public static class GetTicketTypesHttpEndpoint
{
    public static RouteGroupBuilder MapGetTicketTypes(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetTicketTypes)
            .WithName(nameof(GetTicketTypes))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<TicketTypeDto>>> GetTicketTypes(
        Guid teamId,
        Guid eventId,
        IQueryHandler<GetTicketTypesQuery, IReadOnlyList<TicketTypeDto>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetTicketTypesQuery(TicketedEventId.From(eventId), TeamId.From(teamId));

        var result = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
