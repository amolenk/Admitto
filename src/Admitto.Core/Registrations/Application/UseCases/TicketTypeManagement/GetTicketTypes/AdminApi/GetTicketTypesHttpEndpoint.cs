using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.GetTicketTypes.AdminApi;

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
        GetTicketTypesHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetTicketTypesQuery(TicketedEventId.From(eventId));

        var result = await handler.HandleAsync(query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
